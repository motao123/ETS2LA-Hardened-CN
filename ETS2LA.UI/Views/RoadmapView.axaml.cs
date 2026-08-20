using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views;

public partial class RoadmapView : UserControl
{
    public RoadmapView()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationManager.Localize(this);
    }

    private void OpenLink(string url)
    {
        # if LINUX
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
        new Process
        {
            StartInfo = new ProcessStartInfo(url) { UseShellExecute = true }
        }.Start();
        # endif
    }

    public void OpenGitHub(object? sender, RoutedEventArgs e) => OpenLink("https://github.com/motao123/ETS2LA-Hardened-CN");
    public void OpenOriginalProject(object? sender, RoutedEventArgs e) => OpenLink("https://github.com/ETS2LA/ETS2LA");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}