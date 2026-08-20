using System.IO.Compression;

namespace ETS2LA.Networking.Plugins;

public static class SafeArchiveExtractor
{
    public const int MaximumEntryCount = 2048;
    public const long MaximumExtractedBytes = 512L * 1024 * 1024;

    public static void ExtractZip(string archivePath, string destinationRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        var root = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(root);

        using var archive = ZipFile.OpenRead(archivePath);
        if (archive.Entries.Count > MaximumEntryCount)
            throw new InvalidDataException($"Archive contains more than {MaximumEntryCount} entries.");

        long declaredTotalBytes = 0;
        long actualTotalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            ValidateEntry(entry);
            declaredTotalBytes = checked(declaredTotalBytes + entry.Length);
            if (declaredTotalBytes > MaximumExtractedBytes)
                throw new InvalidDataException($"Archive expands beyond {MaximumExtractedBytes} bytes.");

            var normalizedName = entry.FullName
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace((char)92, Path.DirectorySeparatorChar);
            var outputPath = PluginSecurityPaths.GetPathInsideRoot(root, normalizedName);
            if (IsDirectory(entry))
            {
                Directory.CreateDirectory(outputPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            ExtractEntry(entry, outputPath, ref actualTotalBytes);
        }
    }

    private static void ValidateEntry(ZipArchiveEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.IndexOf('\0') >= 0 ||
            entry.FullName.StartsWith('/') || entry.FullName.StartsWith((char)92) ||
            entry.FullName.Contains(':') || Path.IsPathFullyQualified(entry.FullName) || Path.IsPathRooted(entry.FullName))
            throw new InvalidDataException($"Archive entry '{entry.FullName}' has an invalid path.");

        var segments = entry.FullName.Split(new[] { '/', (char)92 }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
            throw new InvalidDataException($"Archive entry '{entry.FullName}' contains directory traversal.");

        var unixType = (entry.ExternalAttributes >> 16) & 0xF000;
        if (unixType == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException($"Archive entry '{entry.FullName}' is a symbolic link or reparse point.");
    }

    private static bool IsDirectory(ZipArchiveEntry entry) =>
        entry.FullName.EndsWith('/') || entry.FullName.EndsWith((char)92);

    private static void ExtractEntry(ZipArchiveEntry entry, string outputPath, ref long actualTotalBytes)
    {
        using var input = entry.Open();
        using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        long entryBytes = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
        {
            entryBytes = checked(entryBytes + read);
            actualTotalBytes = checked(actualTotalBytes + read);
            if (actualTotalBytes > MaximumExtractedBytes)
                throw new InvalidDataException($"Archive expands beyond {MaximumExtractedBytes} bytes.");
            output.Write(buffer, 0, read);
        }

        if (entryBytes != entry.Length)
            throw new InvalidDataException($"Archive entry '{entry.FullName}' size does not match its metadata.");
    }
}
