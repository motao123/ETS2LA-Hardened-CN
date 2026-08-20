using ETS2LA.Networking.Settings;
using ETS2LA.Backend;
using ETS2LA.Backend.Events;
using ETS2LA.Backend.Plugins;
using ETS2LA.Notifications;
using ETS2LA.Logging;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace ETS2LA.Networking.Plugins;

public class PluginApiClient
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
    private static readonly SemaphoreSlim InstallLock = new(1, 1);
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public List<NetworkPlugin> AvailablePlugins { get; private set; } = new();

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private void Log(string message, NotificationLevel level = NotificationLevel.Information)
    {
        switch (level)
        {
            case NotificationLevel.Information: Logger.Info(message); break;
            case NotificationLevel.Warning: Logger.Warn(message); break;
            case NotificationLevel.Danger: Logger.Error(message); break;
            case NotificationLevel.Success: Logger.Success(message); break;
            default: Logger.Info(message); break;
        }

        NotificationHandler.Current.SendNotification(new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Plugin Installer",
            Content = message,
            Level = level
        });
    }

    public async Task FetchAvailablePluginsAsync()
    {
        try
        {
            var apiServer = NetworkingSettings.Current.CurrentApiServer
                ?? throw new InvalidOperationException("CurrentApiServer is not set.");
            using var response = await HttpClient.GetAsync(
                $"{apiServer.BaseUrl}/plugins",
                HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            AvailablePlugins = await JsonSerializer.DeserializeAsync<List<NetworkPlugin>>(content, jsonOptions).ConfigureAwait(false) ?? new();
            Log($"Fetched {AvailablePlugins.Count} plugins from {apiServer.BaseUrl}");
        }
        catch (Exception ex)
        {
            Log($"Failed to fetch available plugins: {ex.Message}", NotificationLevel.Danger);
        }
    }

    public bool PluginHasUpdateAvailable(string pluginId)
    {
        try
        {
            PluginSecurityPaths.ValidatePluginId(pluginId);
            var plugin = AvailablePlugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null)
            {
                Log($"Plugin with ID {pluginId} not found in available plugins.", NotificationLevel.Warning);
                return false;
            }

            InstalledPlugin? installed = FindInstalledPlugin(pluginId);
            if (!installed.HasValue || string.IsNullOrEmpty(installed.Value.Version)) return false;
            var latest = plugin.GetLatestCompatibleVersion(GetAppVersion(), GetCurrentOperatingSystem());
            if (latest == null || string.IsNullOrEmpty(latest.Version))
            {
                Log($"No valid versions found for plugin with ID {pluginId}.", NotificationLevel.Warning);
                return false;
            }
            return new Version(latest.Version) > new Version(installed.Value.Version);
        }
        catch (Exception ex)
        {
            Log($"Failed to check updates for plugin {pluginId}: {ex.Message}", NotificationLevel.Danger);
            throw;
        }
    }

    public bool InstallPlugin(string pluginId) => InstallPluginAsync(pluginId).GetAwaiter().GetResult();

    public async Task<bool> InstallPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            await InstallLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var plan = PluginDependencyResolver.Resolve(
                    AvailablePlugins,
                    InstalledPluginManifest.Current.InstalledPlugins.Select(p => p.Id),
                    pluginId,
                    GetAppVersion(),
                    GetCurrentOperatingSystem());
                foreach (var item in plan)
                {
                    var isUpdate = InstalledPluginManifest.Current.InstalledPlugins.Any(p => p.Id == item.Plugin.Id);
                    await InstallResolvedPluginAsync(item, isUpdate, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                InstallLock.Release();
            }
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to install plugin {pluginId}: {ex.Message}", NotificationLevel.Danger);
            throw;
        }
    }

    public bool UpdatePlugin(string pluginId) => UpdatePluginAsync(pluginId).GetAwaiter().GetResult();

    public async Task<bool> UpdatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!PluginHasUpdateAvailable(pluginId))
        {
            Log($"No update available for plugin with ID {pluginId}.", NotificationLevel.Information);
            return false;
        }

        try
        {
            return await InstallPluginAsync(pluginId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log($"Failed to update plugin {pluginId}: {ex.Message}", NotificationLevel.Danger);
            throw;
        }
    }

    public bool UninstallPlugin(string pluginId, bool overrideDependencyCheck = false)
    {
        try
        {
            PluginSecurityPaths.ValidatePluginId(pluginId);
            InstalledPlugin? installed = FindInstalledPlugin(pluginId);
            if (installed == null)
            {
                Log($"Installed plugin with ID {pluginId} not found.", NotificationLevel.Warning);
                return false;
            }

            if (!overrideDependencyCheck)
            {
                var dependents = InstalledPluginManifest.Current.InstalledPlugins
                    .Where(p => (p.Dependencies ?? new List<string>()).Contains(installed.Value.Id)).ToList();
                if (dependents.Count > 0)
                {
                    Log($"Cannot uninstall plugin with ID {pluginId} because these installed plugins depend on it: {string.Join(", ", dependents.Select(p => p.Id))}", NotificationLevel.Warning);
                    return false;
                }
            }

            var pluginRoot = GetPluginRoot();
            var folder = installed.Value.Type == PluginType.Plugin ? "Plugins" : "Libraries";
            var folderRoot = PluginSecurityPaths.GetPathInsideRoot(pluginRoot, folder);
            var pluginPath = PluginSecurityPaths.GetPathInsideRoot(folderRoot, installed.Value.Id);
            PluginSecurityPaths.EnsurePathInsideRoot(folderRoot, pluginPath);
            if (!Directory.Exists(pluginPath))
            {
                Log($"Apparent plugin directory {pluginPath} does not exist.", NotificationLevel.Warning);
                return false;
            }

            var backupPath = PluginSecurityPaths.GetPathInsideRoot(
                folderRoot,
                $".{installed.Value.Id}.uninstall-{Guid.NewGuid():N}");
            Directory.Move(pluginPath, backupPath);
            InstalledPluginManifest.Current.InstalledPlugins.Remove(installed.Value);
            try
            {
                SaveAndVerifyManifest(pluginId, expectedVersion: null);
            }
            catch (Exception uninstallException)
            {
                InstalledPluginManifest.Current.InstalledPlugins.Add(installed.Value);
                try
                {
                    SaveAndVerifyManifest(pluginId, installed.Value.Version);
                    Directory.Move(backupPath, pluginPath);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Plugin uninstall and rollback both failed.", uninstallException, rollbackException);
                }
                throw;
            }
            Directory.Delete(backupPath, true);
            Events.Current.Publish<string>("ETS2LA.Plugins.Uninstalled", pluginId);
            Events.Current.Publish<EventArgs>($"ETS2LA.Plugins.Uninstalled.{pluginId}", EventArgs.Empty);
            Log($"Successfully uninstalled plugin with ID {pluginId}", NotificationLevel.Success);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to uninstall plugin {pluginId}: {ex.Message}", NotificationLevel.Danger);
            throw;
        }
    }

    private async Task InstallResolvedPluginAsync(
        ResolvedNetworkPlugin resolved,
        bool isUpdate,
        CancellationToken cancellationToken)
    {
        var plugin = resolved.Plugin;
        var version = resolved.Version;
        PluginSecurityPaths.ValidatePluginId(plugin.Id);
        PluginSecurityPaths.ValidateRelativeDllPath(version.DllPath);
        ValidatePackageTrust(version, plugin.Id);
        var downloadUri = ResolveDownloadUri(version);

        var pluginRoot = GetPluginRoot();
        var pluginType = plugin.Tags.Contains(NetworkPluginTags.Plugin) ? PluginType.Plugin : PluginType.Library;
        InstalledPlugin? previous = FindInstalledPlugin(plugin.Id);
        if (previous.HasValue && previous.Value.Type != pluginType)
            throw new InvalidOperationException($"Plugin '{plugin.Id}' cannot change its installed type during an update.");
        var folder = pluginType == PluginType.Plugin ? "Plugins" : "Libraries";
        var folderRoot = PluginSecurityPaths.GetPathInsideRoot(pluginRoot, folder);
        Directory.CreateDirectory(folderRoot);
        var outputPath = PluginSecurityPaths.GetPathInsideRoot(folderRoot, plugin.Id);
        var transactionId = Guid.NewGuid().ToString("N");
        var stagingPath = PluginSecurityPaths.GetPathInsideRoot(folderRoot, $".{plugin.Id}.staging-{transactionId}");
        var backupPath = PluginSecurityPaths.GetPathInsideRoot(folderRoot, $".{plugin.Id}.backup-{transactionId}");
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"ets2la-plugin-{transactionId}.zip");
        var movedOld = false;
        var movedNew = false;
        var committed = false;

        try
        {
            await DownloadAndVerifyAsync(downloadUri, tempFilePath, version.Sha256, cancellationToken).ConfigureAwait(false);
            SafeArchiveExtractor.ExtractZip(tempFilePath, stagingPath);
            var stagingDllPath = PluginSecurityPaths.GetPathInsideRoot(stagingPath, version.DllPath);
            if (!File.Exists(stagingDllPath))
                throw new InvalidDataException($"Plugin archive does not contain required DLL '{version.DllPath}'.");
            RejectReparsePoints(stagingPath);

            if (Directory.Exists(outputPath))
            {
                Directory.Move(outputPath, backupPath);
                movedOld = true;
            }
            Directory.Move(stagingPath, outputPath);
            movedNew = true;

            var installed = new InstalledPlugin
            {
                Id = plugin.Id,
                Version = version.Version,
                Dependencies = version.Dependencies ?? new List<string>(),
                DllPath = PluginSecurityPaths.GetPathInsideRoot(outputPath, version.DllPath),
                Type = pluginType
            };
            ReplaceManifestEntry(previous, installed);
            try
            {
                SaveAndVerifyManifest(plugin.Id, version.Version);
                committed = true;
            }
            catch (Exception commitException)
            {
                RestoreManifestEntry(previous, installed);
                try
                {
                    SaveAndVerifyManifest(plugin.Id, previous?.Version);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Plugin manifest update and rollback both failed.", commitException, rollbackException);
                }
                throw;
            }

            var eventName = isUpdate ? "Updated" : "Installed";
            Events.Current.Publish<string>($"ETS2LA.Plugins.{eventName}", plugin.Id);
            Events.Current.Publish<EventArgs>($"ETS2LA.Plugins.{eventName}.{plugin.Id}", EventArgs.Empty);
            Log($"Successfully {(isUpdate ? "updated" : "installed")} plugin {plugin.Name} ({plugin.Id}, {version.Version})", NotificationLevel.Success);
        }
        catch (Exception installException)
        {
            if (!committed)
            {
                try
                {
                    RollBackDirectories(outputPath, backupPath, movedNew, movedOld);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Plugin installation and directory rollback both failed.", installException, rollbackException);
                }
            }
            throw;
        }
        finally
        {
            DeleteDirectoryIfPresent(stagingPath);
            if (committed) DeleteDirectoryIfPresent(backupPath);
            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
        }
    }

    private static async Task DownloadAndVerifyAsync(
        Uri downloadUri,
        string destinationPath,
        string? expectedSha256,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri is not Uri finalUri ||
            !string.Equals(finalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Plugin download redirected to a non-HTTPS URL.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            hash.AppendData(buffer, 0, read);
        }

        var actualBytes = hash.GetHashAndReset();
        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            var expectedBytes = Convert.FromHexString(expectedSha256);
            if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
                throw new CryptographicException($"Plugin package SHA-256 mismatch. Expected {expectedSha256}, received {Convert.ToHexString(actualBytes)}.");
        }
    }

    private static void ValidatePackageTrust(NetworkPluginVersion version, string pluginId)
    {
        if (string.IsNullOrWhiteSpace(version.Sha256))
        {
            Logger.Warn($"Plugin '{pluginId}' has no SHA-256 digest; installing over HTTPS without digest verification.");
        }
        else if (version.Sha256.Length != 64 || !version.Sha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException($"Plugin '{pluginId}' has an invalid SHA-256 digest.");
        }
        if (version.Signature is not null || version.SignerId is not null)
            throw new InvalidOperationException($"Plugin '{pluginId}' declares a signature, but no trusted signer configuration is available.");
    }

    private static Uri ResolveDownloadUri(NetworkPluginVersion version)
    {
        var region = NetworkingSettings.Current.CurrentApiServer?.Name == "China" ? Region.China : Region.Global;
        if (!version.DownloadUrl.TryGetValue(region, out var url) || string.IsNullOrWhiteSpace(url))
            version.DownloadUrl.TryGetValue(Region.Global, out url);
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException($"No download URL is available for region {region}.");
        return PluginSecurityPaths.ValidateHttpsUrl(url);
    }

    private static string GetPluginRoot()
    {
        var root = PluginBackend.Current.PluginHandler?.PluginRootPath
            ?? throw new InvalidOperationException("Plugin handler is not initialized.");
        return Path.GetFullPath(string.IsNullOrWhiteSpace(root) ? Directory.GetCurrentDirectory() : root);
    }

    private static void RejectReparsePoints(string root)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Extracted path '{path}' is a reparse point.");
        }
    }

    private static void ReplaceManifestEntry(InstalledPlugin? previous, InstalledPlugin installed)
    {
        if (previous.HasValue) InstalledPluginManifest.Current.InstalledPlugins.Remove(previous.Value);
        InstalledPluginManifest.Current.InstalledPlugins.Add(installed);
    }

    private static void RestoreManifestEntry(InstalledPlugin? previous, InstalledPlugin installed)
    {
        InstalledPluginManifest.Current.InstalledPlugins.Remove(installed);
        if (previous.HasValue) InstalledPluginManifest.Current.InstalledPlugins.Add(previous.Value);
    }

    private static InstalledPlugin? FindInstalledPlugin(string pluginId)
    {
        foreach (var installed in InstalledPluginManifest.Current.InstalledPlugins)
        {
            if (installed.Id == pluginId) return installed;
        }
        return null;
    }

    private static void RollBackDirectories(
        string outputPath,
        string backupPath,
        bool movedNew,
        bool movedOld)
    {
        if (movedNew && Directory.Exists(outputPath)) Directory.Delete(outputPath, true);
        if (movedOld && Directory.Exists(backupPath)) Directory.Move(backupPath, outputPath);
    }

    private static void SaveAndVerifyManifest(string pluginId, string? expectedVersion)
    {
        InstalledPluginManifest.Current.Save();
        var manifestPath = Path.Combine(
            ETS2LA.Settings.SettingsHandler.ConfigurationDirectory,
            "InstalledPluginManifest.json");
        var saved = JsonSerializer.Deserialize<InstalledPluginManifest>(File.ReadAllText(manifestPath), ManifestJsonOptions)
            ?? throw new IOException("Saved plugin manifest could not be read.");
        var entry = saved.InstalledPlugins.FirstOrDefault(p => p.Id == pluginId);
        var persisted = expectedVersion == null
            ? string.IsNullOrEmpty(entry.Id)
            : entry.Id == pluginId && entry.Version == expectedVersion;
        if (!persisted)
            throw new IOException($"Plugin manifest did not persist the expected state for '{pluginId}'.");
    }

    private static void DeleteDirectoryIfPresent(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }

    private static string GetAppVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    private static OperatingSystem GetCurrentOperatingSystem()
    {
        if (System.OperatingSystem.IsWindows()) return OperatingSystem.Windows;
        if (System.OperatingSystem.IsMacOS()) return OperatingSystem.MacOS;
        return OperatingSystem.Linux;
    }
}
