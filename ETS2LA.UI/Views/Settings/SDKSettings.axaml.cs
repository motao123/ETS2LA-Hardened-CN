using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ComponentModel;

using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using ETS2LA.Game;
using ETS2LA.Notifications;
using ETS2LA.Logging;
using ETS2LA.UI.Localization;

namespace ETS2LA.UI.Views.Settings;

public partial class SDKSettings : UserControl
{
    public ObservableCollection<GameItem> Games { get; } = new();

    public SDKSettings()
    {
        InitializeComponent();
        ETS2LA.UI.Localization.LocalizationManager.Localize(this);

        DataContext = this;
        UpdateGamesList();
    }

    private void UpdateGamesList()
    {
        Games.Clear();
        foreach (var installation in GameHandler.Current.Installations)
        {
            Games.Add(new GameItem(installation));
        }
    }

    private void OnTriggerChange(object? sender, PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
            return;

        if (sender is Control { Tag: GameItem item })
        {
            Task.Run(() => item.TriggerChange());
        }
    }

    private void OnTriggerChangeKey(object? sender, KeyEventArgs e)
    {
        if (sender is Control { Tag: GameItem item })
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                Task.Run(() => item.TriggerChange());
            }
        }
    }

    private async void OnAddGameManually(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationManager.Get("SelectGameFolder"),
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        string? gamePath = folders[0].TryGetLocalPath();
        var installation = gamePath != null ? GameHandler.Current.AddManualInstallation(gamePath) : null;
        if (installation == null)
        {
            NotificationHandler.Current.SendNotification(new Notification
            {
                Id = "ETS2LA.UI.SDKSettings.AddGameFailed",
                Title = LocalizationManager.Get("CouldNotAddGame"),
                Content = LocalizationManager.Get("NoGameExecutableFound"),
                Level = NotificationLevel.Danger
            });
            return;
        }

        UpdateGamesList();
    }

    private void OnRemoveGame(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: GameItem item })
        {
            GameHandler.Current.RemoveManualInstallation(item.Installation);
            UpdateGamesList();
        }
    }
}

public class GameItem : INotifyPropertyChanged
{
    public string Name => GetName();
    public string Version => installation.Version;
    public string UpdatedVersion { get; set; } = string.Empty;
    public string Path => installation.Path;
    public bool IsUnknownVersion => !Version.Contains(".");
    public bool IsSDKInstalled => installation.IsSDKInstalled(IsUnknownVersion ? UpdatedVersion : Version);
    public bool IsManuallyAdded => installation.IsManuallyAdded;

    public string AutomationName => GetAutomationName();

    public Installation Installation => installation;

    private Installation installation;

    public GameItem(Installation installation)
    {
        this.installation = installation;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void TriggerChange()
    {
        if (IsSDKInstalled)
        {
            if (installation.UninstallSDK(IsUnknownVersion ? UpdatedVersion : Version))
            {
                Logger.Info($"Uninstalled SDK for {Name} at {Path}");
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "ETS2LA.UI.SDKSettings.Uninstall",
                    Title = LocalizationManager.Format("SDKUninstalledTitle", Name),
                    Content = LocalizationManager.Format("SDKUninstalledContent", Name, Path),
                    Level = NotificationLevel.Success
                });
            }
            else
            {
                Logger.Error($"Failed to uninstall SDK for {Name} at {Path}");
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "ETS2LA.UI.SDKSettings.UninstallFailed",
                    Title = LocalizationManager.Format("SDKUninstallFailedTitle", Name),
                    Content = LocalizationManager.Format("SDKUninstallFailedContent", Name, Path),
                    Level = NotificationLevel.Danger
                });
            }
        }
        else
        {
            if (installation.InstallSDK(IsUnknownVersion ? UpdatedVersion : Version))
            {
                Logger.Info($"Installed SDK for {Name} at {Path}");
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "ETS2LA.UI.SDKSettings.Install",
                    Title = LocalizationManager.Format("SDKInstalledTitle", Name),
                    Content = LocalizationManager.Format("SDKInstalledContent", Name, Path),
                    Level = NotificationLevel.Success
                });
            }
            else
            {
                Logger.Error($"Failed to install SDK for {Name} at {Path}");
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "ETS2LA.UI.SDKSettings.InstallFailed",
                    Title = LocalizationManager.Format("SDKInstallFailedTitle", Name),
                    Content = LocalizationManager.Format("SDKInstallFailedContent", Name, Path),
                    Level = NotificationLevel.Danger
                });
            }
        }

        OnPropertyChanged(nameof(IsSDKInstalled));
    }

    private string GetName()
    {
        return installation.Type == GameType.EuroTruckSimulator2 ? "Euro Truck Simulator 2" : "American Truck Simulator";
    }

    private string GetAutomationName()
    {
        var status = LocalizationManager.TranslateLiteral(IsSDKInstalled ? "已安装" : "未安装");
        return $"{Name} {Version}, SDK: {status} at {Path}, button";
    }
}
