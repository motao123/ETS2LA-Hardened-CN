using System.Security.Cryptography;
using System.Text;

namespace ETS2LA.Networking.Users;

public interface ISecretStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Remove(string key);
}

public sealed class ProtectedSecretStore : ISecretStore
{
    private static readonly Lazy<ProtectedSecretStore> Instance = new(() => new());
    public static ProtectedSecretStore Current => Instance.Value;

    private readonly string filePath = Path.Combine(
        ETS2LA.Settings.SettingsHandler.ConfigurationDirectory,
        "secrets.dat");
    private readonly object sync = new();
    private Dictionary<string, string>? values;

    public string? Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (sync)
        {
            Load();
            return values!.TryGetValue(key, out var value) ? value : null;
        }
    }

    public void Set(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        lock (sync)
        {
            Load();
            values![key] = value;
            Save();
        }
    }

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (sync)
        {
            Load();
            if (values!.Remove(key)) Save();
        }
    }

    private void Load()
    {
        if (values != null) return;
        values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!OperatingSystem.IsWindows() || !File.Exists(filePath)) return;

        try
        {
            var payload = File.ReadAllBytes(filePath);
            var plaintext = Unprotect(payload);
            foreach (var line in Encoding.UTF8.GetString(plaintext).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = line.IndexOf('=');
                if (separator > 0)
                    values[line[..separator]] = line[(separator + 1)..];
            }
        }
        catch
        {
            values.Clear();
        }
    }

    private void Save()
    {
        if (!OperatingSystem.IsWindows())
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var content = string.Join('\n', values!.Select(pair => $"{pair.Key}={pair.Value}"));
        var protectedBytes = Protect(Encoding.UTF8.GetBytes(content));
        var temp = filePath + ".tmp";
        File.WriteAllBytes(temp, protectedBytes);
        File.Move(temp, filePath, overwrite: true);
    }

    private static byte[] Protect(byte[] bytes)
    {
        if (OperatingSystem.IsWindows())
            return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);

        // Non-Windows fallback is session-only and never persists bearer tokens.
        return bytes;
    }

    private byte[] Unprotect(byte[] bytes)
    {
        if (OperatingSystem.IsWindows())
            return ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);

        return bytes;
    }
}
