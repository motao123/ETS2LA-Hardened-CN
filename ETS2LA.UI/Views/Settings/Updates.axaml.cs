using Avalonia.Controls;
using Huskui.Avalonia.Models;

using ETS2LA.Shared;
using ETS2LA.Networking.Updates;
using ETS2LA.Notifications;
using ETS2LA.UI.Localization;

using Velopack;
using System.ComponentModel;

namespace ETS2LA.UI.Views.Settings;

public partial class Updates : UserControl, INotifyPropertyChanged
{
    private Updater _updater;

    public string RepositoryUrl => "https://github.com/motao123/ETS2LA-Hardened-CN";
    public string CurrentVersion { get; set; } = "Unknown";
    public bool IsUpdateAvailable => LatestUpdateInfo != null;
    public string LatestVersion => LatestUpdateInfo != null ? $"v{LatestUpdateInfo.TargetFullRelease.Version}" : "N/A";
    public string ReleaseNotes => GetReleaseNotes();

    public UpdateInfo? LatestUpdateInfo { get; set; }
    public new event PropertyChangedEventHandler? PropertyChanged;

    public Updates()
    {
        _updater = Updater.Current;
        CurrentVersion = $"v{_updater.UpdateManager.CurrentVersion}";
        InitializeComponent();
        ETS2LA.UI.Localization.LocalizationManager.Localize(this);

        DataContext = this;
        MainWindow.WindowOpened += (s, e) => OnCheckForUpdatesClick(this, new Avalonia.Interactivity.RoutedEventArgs());
    }

    private string GetReleaseNotes()
    {
        if(LatestUpdateInfo == null)
        {
            return "没有可用的发行说明。";
        }

        if (string.IsNullOrEmpty(LatestUpdateInfo.TargetFullRelease.NotesMarkdown))
        {
            return "没有可用的发行说明。";
        }

        string notes = LatestUpdateInfo.TargetFullRelease.NotesMarkdown;
        
        // Remove GitHub only section if present
        try { notes = notes.Split("---")[0]; }
        catch { }

        return notes;
    }

    public void OnCheckForUpdatesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NotificationHandler.Current.SendNotification(new Notification
        {
            Id = "UpdateNotification",
            Title = LocalizationManager.Get("CheckingUpdates"),
            Content = LocalizationManager.Get("CheckingUpdatesContent"),
            Level = NotificationLevel.Information,
            CloseAfter = 0,
            IsProgressIndeterminate = true
        });
        Task.Run(() => {
            LatestUpdateInfo = _updater.CheckForUpdates();
            if (LatestUpdateInfo != null)
            {
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "UpdateNotification",
                    Title = LocalizationManager.Get("UpdateAvailable"),
                    Content = LocalizationManager.Format("UpdateAvailableContent", LatestUpdateInfo.TargetFullRelease.Version),
                    Level = NotificationLevel.Success,
                    CloseAfter = 5,
                    IsProgressIndeterminate = false
                });
                OnPropertyChanged(nameof(IsUpdateAvailable));
                OnPropertyChanged(nameof(LatestVersion));
                OnPropertyChanged(nameof(LatestUpdateInfo));
                OnPropertyChanged(nameof(ReleaseNotes));
            }
            else
            {
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "UpdateNotification",
                    Title = LocalizationManager.Get("NoUpdates"),
                    Content = LocalizationManager.Get("NoUpdatesContent"),
                    Level = NotificationLevel.Information,
                    CloseAfter = 5,
                    IsProgressIndeterminate = false
                });
            }
        });
    }

    private void DownloadCallback(int progress)
    {
        NotificationHandler.Current.SendNotification(new Notification
        {
            Id = "UpdateDownloadProgress",
            Title = LocalizationManager.Get("DownloadingUpdate"),
            Content = LocalizationManager.Format("DownloadProgress", progress),
            Level = NotificationLevel.Information,
            Progress = progress,
            CloseAfter = 0
        });
    }

    public void OnInstallAndRestartClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (LatestUpdateInfo != null)
        {
            Task.Run(() => 
            {
                NotificationHandler.Current.SendNotification(new Notification
                {
                    Id = "UpdateDownloadProgress",
                    Title = LocalizationManager.Get("DownloadingUpdate"),
                    Content = LocalizationManager.Get("StartingDownload"),
                    Level = NotificationLevel.Information,
                    Progress = 0,
                    CloseAfter = 0
                });
                _updater.DownloadUpdates(LatestUpdateInfo, DownloadCallback);
                _updater.ApplyUpdatesAndRestart(LatestUpdateInfo);
                NotificationHandler.Current.CloseNotification("UpdateDownloadProgress");
            });
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
