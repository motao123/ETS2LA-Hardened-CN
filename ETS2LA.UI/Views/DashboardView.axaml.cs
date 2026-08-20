using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ETS2LA.UI.Localization;
using System.Diagnostics;
using ETS2LA.Logging;

namespace ETS2LA.UI.Views;

public partial class DashboardView : UserControl, INotifyPropertyChanged
{

    public string CurrentRelease { get; set; } = "Unknown";
    public int UsersOnline { get; set; } = 123;
    public string WelcomeText => string.Format(LocalizationManager.Get("WelcomeBack"), "Anonymous");
    public string CurrentReleaseLabel => LocalizationManager.Get("CurrentRelease");
    public string UsersOnlineLabel => LocalizationManager.Get("UsersOnline");
    public string UsersOver24hLabel => LocalizationManager.Get("UsersOver24h");
    public string SupportUpdatesLabel => LocalizationManager.Get("SupportUpdates");
    public string SupportUpdatesDescription => LocalizationManager.Get("SupportUpdatesDescription");
    public string DocumentationLabel => LocalizationManager.Get("Documentation");
    public string GitHubLabel => LocalizationManager.Get("GitHub");
    public string OriginalProjectLabel => LocalizationManager.TranslateLiteral("原始项目");
    public string OriginalProjectDescription => LocalizationManager.TranslateLiteral("本项目基于 ETS2LA 开发，感谢原作者和所有贡献者。");
    public string OriginalProjectLinkLabel => LocalizationManager.TranslateLiteral("访问原作者项目");

    public DashboardView()
    {
        CurrentRelease = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)}";
        InitializeComponent();
        DataContext = this;
        LocalizationManager.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WelcomeText));
            OnPropertyChanged(nameof(CurrentReleaseLabel));
            OnPropertyChanged(nameof(UsersOnlineLabel));
            OnPropertyChanged(nameof(UsersOver24hLabel));
            OnPropertyChanged(nameof(SupportUpdatesLabel));
            OnPropertyChanged(nameof(SupportUpdatesDescription));
            OnPropertyChanged(nameof(DocumentationLabel));
            OnPropertyChanged(nameof(GitHubLabel));
            OnPropertyChanged(nameof(OriginalProjectLabel));
            OnPropertyChanged(nameof(OriginalProjectDescription));
            OnPropertyChanged(nameof(OriginalProjectLinkLabel));
            LocalizationManager.Localize(this);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void OpenLink(string url)
    {
        # if LINUX
            // Linux doesn't support Process.Start with UseShellExecute, so we need to use xdg-open.
            new Process
            {
                StartInfo = new ProcessStartInfo("xdg-open", url)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            }.Start();
        # else
            // Windows and macOS can use the default method.
            new Process
            {
                StartInfo = new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                }
            }.Start();
        # endif
    }

    public void OpenGitHub(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://github.com/motao123/ETS2LA-Hardened-CN");
    }

    public void OpenDocumentation(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://motao123.github.io/ETS2LA-Hardened-CN/");
    }

    public void OpenOriginalProject(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://github.com/ETS2LA/ETS2LA");
    }

}
