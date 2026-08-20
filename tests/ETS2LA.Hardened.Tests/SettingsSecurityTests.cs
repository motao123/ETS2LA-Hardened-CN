using System.Text.Json;
using ETS2LA.Networking.Users;
using ETS2LA.Settings;

namespace ETS2LA.Hardened.Tests;

public sealed class SettingsSecurityTests
{
    [Fact]
    public void SettingsHandler_RejectsPathTraversal()
    {
        var previous = Environment.GetEnvironmentVariable("ETS2LA_CONFIG_DIR");
        var root = Path.Combine(Path.GetTempPath(), "ets2la-settings-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("ETS2LA_CONFIG_DIR", root);
        try
        {
            using var handler = new SettingsHandler();
            Assert.Throws<ArgumentException>(() => handler.Save("../outside.json", new { Value = 1 }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ETS2LA_CONFIG_DIR", previous);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UserJwtToken_IsNotSerializedIntoSettingsJson()
    {
        var user = new User { JwtToken = "secret-token" };
        var json = JsonSerializer.Serialize(user);
        Assert.DoesNotContain("secret-token", json, StringComparison.Ordinal);
        Assert.DoesNotContain("JwtToken", json, StringComparison.Ordinal);
    }
}
