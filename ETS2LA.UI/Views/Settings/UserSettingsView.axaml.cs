using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using ETS2LA.Settings.Global;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views.Settings;

public partial class UserSettingsView : UserControl, INotifyPropertyChanged
{
    public bool NeedsRestart { get; private set; }
    public bool LanguageNeedsRestart { get; private set; }
    public string LanguageLabel => LocalizationManager.Get("Language");
    public string LanguageRestartLabel => LocalizationManager.Get("LanguageRestart");

    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new()
    {
        new() { Value = UiLanguage.ChineseSimplified, DisplayName = "中文" },
        new() { Value = UiLanguage.English, DisplayName = "English" }
    };

    public string LanguageOptionsText => string.Join(" / ", LanguageOptions.Select(option => option.DisplayName));

    public LanguageOption SelectedLanguage
    {
        get => LanguageOptions.First(option => option.Value == LocalizationManager.Current);
        set
        {
            if (value.Value == LocalizationManager.Current) return;
            LocalizationManager.Set(value.Value);
            LanguageNeedsRestart = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageNeedsRestart));
        }
    }

    public bool IsTelemetryEnabled
    {
        get => UserSettings.Current.IsTelemetryEnabled;
        set
        {
            if (UserSettings.Current.IsTelemetryEnabled == value) return;
            UserSettings.Current.IsTelemetryEnabled = value;
            UserSettings.Current.Save();
            NeedsRestart = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NeedsRestart));
        }
    }

    public UserSettingsView()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationManager.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(LanguageRestartLabel));
            OnPropertyChanged(nameof(SelectedLanguage));
            LocalizationManager.Localize(this);
        };
        LocalizationManager.Localize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
