using System.Globalization;

namespace ETS2LA.UI.Localization;

public static class UiText
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AppTitle"] = "ETS2LA Hardened CN",
        ["UnknownVersion"] = "Unknown Version",
        ["SmokeTestPassed"] = "Smoke test passed"
    };

    private static readonly IReadOnlyDictionary<string, string> SimplifiedChinese = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AppTitle"] = "ETS2LA 安全加固版",
        ["UnknownVersion"] = "未知版本",
        ["SmokeTestPassed"] = "启动检查通过"
    };

    public static string Get(string key, CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        culture ??= CultureInfo.CurrentUICulture;
        var resources = culture.Name.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase)
            ? SimplifiedChinese
            : English;
        return resources.TryGetValue(key, out var value)
            ? value
            : English.TryGetValue(key, out var fallback) ? fallback : key;
    }
}
