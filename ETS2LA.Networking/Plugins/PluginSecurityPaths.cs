using System.Text.RegularExpressions;

namespace ETS2LA.Networking.Plugins;

public static partial class PluginSecurityPaths
{
    [GeneratedRegex("^[a-z0-9]+(?:[.-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PluginIdPattern();

    public static string ValidatePluginId(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        if (!PluginIdPattern().IsMatch(pluginId) || pluginId.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Plugin ID may contain only lowercase letters, numbers, dots, and hyphens, without empty segments.", nameof(pluginId));

        return pluginId;
    }

    public static string ValidateRelativeDllPath(string dllPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dllPath);
        if (Path.IsPathFullyQualified(dllPath) || Path.IsPathRooted(dllPath) ||
            dllPath.StartsWith((char)92) || dllPath.IndexOfAny(new[] { '\0', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
            throw new ArgumentException("DLL path must be a safe relative path.", nameof(dllPath));

        var segments = dllPath.Split(new[] { '/', (char)92 }, StringSplitOptions.None);
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new ArgumentException("DLL path must not contain empty, current-directory, or parent-directory segments.", nameof(dllPath));
        if (!string.Equals(Path.GetExtension(dllPath), ".dll", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("DLL path must identify a .dll file.", nameof(dllPath));

        return dllPath;
    }

    public static Uri ValidateHttpsUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Plugin download URL must be an absolute HTTPS URL without credentials.", nameof(url));

        return uri;
    }

    public static string GetPathInsideRoot(string rootPath, params string[] relativeParts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (relativeParts.Length == 0)
            throw new ArgumentException("At least one relative path part is required.", nameof(relativeParts));
        if (relativeParts.Any(part => string.IsNullOrWhiteSpace(part) || Path.IsPathFullyQualified(part) || Path.IsPathRooted(part)))
            throw new ArgumentException("All path parts must be relative.", nameof(relativeParts));

        var fullRoot = Path.GetFullPath(rootPath);
        var normalizedParts = relativeParts
            .Select(part => part.Replace('/', Path.DirectorySeparatorChar).Replace((char)92, Path.DirectorySeparatorChar))
            .ToArray();
        var candidate = Path.GetFullPath(Path.Combine(new[] { fullRoot }.Concat(normalizedParts).ToArray()));
        return EnsurePathInsideRoot(fullRoot, candidate);
    }

    public static string EnsurePathInsideRoot(string rootPath, string candidatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);

        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        var fullCandidate = Path.GetFullPath(candidatePath);
        var rootPrefix = fullRoot + Path.DirectorySeparatorChar;
        if (!fullCandidate.StartsWith(rootPrefix, PathComparison))
            throw new InvalidOperationException($"Path '{candidatePath}' is outside the allowed root '{rootPath}'.");

        return fullCandidate;
    }

    private static StringComparison PathComparison =>
        System.OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
