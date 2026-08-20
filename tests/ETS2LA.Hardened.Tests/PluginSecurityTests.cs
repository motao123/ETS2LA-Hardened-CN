using System.IO.Compression;
using ETS2LA.Networking.Plugins;
using PluginOperatingSystem = ETS2LA.Networking.Plugins.OperatingSystem;

namespace ETS2LA.Hardened.Tests;

public sealed class PluginSecurityTests
{
    [Theory]
    [InlineData("author.plugin")]
    [InlineData("author-name.plugin2")]
    public void ValidatePluginId_AcceptsSafeIds(string id)
    {
        Assert.Equal(id, PluginSecurityPaths.ValidatePluginId(id));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("Author.Plugin")]
    [InlineData("author/plugin")]
    [InlineData("author..plugin")]
    public void ValidatePluginId_RejectsUnsafeIds(string id)
    {
        Assert.Throws<ArgumentException>(() => PluginSecurityPaths.ValidatePluginId(id));
    }

    [Fact]
    public void EnsurePathInsideRoot_RejectsTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "ets2la-security-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                PluginSecurityPaths.GetPathInsideRoot(root, "..", "outside.dll"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SafeArchiveExtractor_RejectsTraversalEntry()
    {
        var root = Path.Combine(Path.GetTempPath(), "ets2la-security-tests", Guid.NewGuid().ToString("N"));
        var archivePath = Path.Combine(Path.GetTempPath(), $"ets2la-{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(root);
        try
        {
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../escape.dll");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("not a plugin");
            }

            Assert.Throws<InvalidDataException>(() => SafeArchiveExtractor.ExtractZip(archivePath, root));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(root)!, "escape.dll")));
        }
        finally
        {
            if (File.Exists(archivePath)) File.Delete(archivePath);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SafeArchiveExtractor_ExtractsSafeEntryInsideRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ets2la-security-tests", Guid.NewGuid().ToString("N"));
        var archivePath = Path.Combine(Path.GetTempPath(), $"ets2la-{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(root);
        try
        {
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("nested/plugin.dll");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("safe");
            }

            SafeArchiveExtractor.ExtractZip(archivePath, root);
            Assert.Equal("safe", File.ReadAllText(Path.Combine(root, "nested", "plugin.dll")));
        }
        finally
        {
            if (File.Exists(archivePath)) File.Delete(archivePath);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DependencyResolver_RejectsCycles()
    {
        var a = Plugin("a", "b");
        var b = Plugin("b", "a");

        Assert.Throws<InvalidOperationException>(() => PluginDependencyResolver.Resolve(
            new[] { a, b }, Array.Empty<string>(), "a", "3.4.37", PluginOperatingSystem.Windows));
    }

    private static NetworkPlugin Plugin(string id, params string[] dependencies) => new()
    {
        Id = id,
        Versions = new List<NetworkPluginVersion>
        {
            new()
            {
                Version = "1.0.0",
                AppVersion = "*",
                Dependencies = dependencies.ToList(),
                SupportedOperatingSystems = new List<PluginOperatingSystem> { PluginOperatingSystem.Windows },
                DllPath = "plugin.dll"
            }
        }
    };
}
