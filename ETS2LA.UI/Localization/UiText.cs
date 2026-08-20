using System.Globalization;

namespace ETS2LA.UI.Localization;

public static class UiText
{
    public static string Get(string key, CultureInfo? culture = null) => LocalizationManager.Get(key);
}
