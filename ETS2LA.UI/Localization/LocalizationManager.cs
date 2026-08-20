using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using ETS2LA.Settings;
using ETS2LA.UI.Settings;

namespace ETS2LA.UI.Localization;

public enum UiLanguage
{
    ChineseSimplified,
    English
}

public static class LocalizationManager
{
    private static readonly Dictionary<UiLanguage, IReadOnlyDictionary<string, string>> Resources = new()
    {
        [UiLanguage.ChineseSimplified] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "ETS2LA 安全加固版",
            ["UnknownVersion"] = "未知版本",
            ["Main"] = "主界面",
            ["Dashboard"] = "仪表盘",
            ["Visualization"] = "可视化",
            ["Plugins"] = "插件",
            ["Manager"] = "管理器",
            ["Catalogue"] = "插件目录",
            ["Performance"] = "性能",
            ["Help"] = "帮助",
            ["Wiki"] = "Wiki",
            ["Roadmap"] = "路线图",
            ["Settings"] = "设置",
            ["Language"] = "界面语言",
            ["ChineseSimplified"] = "简体中文",
            ["English"] = "English",
            ["LanguageRestart"] = "切换语言后需要重启程序。"
        },
        [UiLanguage.English] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "ETS2LA Hardened CN",
            ["UnknownVersion"] = "Unknown Version",
            ["Main"] = "Main",
            ["Dashboard"] = "Dashboard",
            ["Visualization"] = "Visualization",
            ["Plugins"] = "Plugins",
            ["Manager"] = "Manager",
            ["Catalogue"] = "Catalogue",
            ["Performance"] = "Performance",
            ["Help"] = "Help",
            ["Wiki"] = "Wiki",
            ["Roadmap"] = "Roadmap",
            ["Settings"] = "Settings",
            ["ChineseSimplified"] = "简体中文",
            ["English"] = "English",
            ["LanguageRestart"] = "Restart the application to apply the language change."
        }
    };

    public static UiLanguage Current
    {
        get
        {
            var value = UISettingsHandler.Current.GetSettings().Language;
            return Enum.TryParse<UiLanguage>(value, out var language) ? language : UiLanguage.ChineseSimplified;
        }
    }

    public static string Get(string key)
    {
        if (Resources[Current].TryGetValue(key, out var value)) return value;
        return Resources[UiLanguage.English].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static void Set(UiLanguage language)
    {
        UISettingsHandler.Current.GetSettings().Language = language.ToString();
        UISettingsHandler.Current.Save();
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static event EventHandler? LanguageChanged;
}

public sealed class LanguageOption
{
    public required UiLanguage Value { get; init; }
    public required string DisplayName { get; init; }
}
