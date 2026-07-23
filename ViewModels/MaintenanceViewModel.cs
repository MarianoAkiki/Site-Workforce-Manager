using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Services;
using System.Threading;

namespace Site_Workforce_Manager.ViewModels;

public partial class MaintenanceViewModel : ObservableObject
{
    [ObservableProperty]
    private string databasePath = AppDbContext.GetDatabasePath();

    [ObservableProperty]
    private string cloudBackupFolder = string.Empty;

    [ObservableProperty]
    private int maxBackupsToKeep = 30;

    [ObservableProperty]
    private bool isAutoBackupPaused;

    [ObservableProperty]
    private string backupStatusMessage = string.Empty;

    [ObservableProperty]
    private string updateStatusMessage = string.Empty;

    [ObservableProperty]
    private bool isCheckingUpdate;

    [ObservableProperty]
    private bool isDownloadingUpdate;

    [ObservableProperty]
    private int downloadProgress;

    [ObservableProperty]
    private bool isUpdateAvailable;

    private ReleaseInfo? _availableRelease;

    public string CurrentVersionText => UpdateService.CurrentVersionText;
    public bool IsNotDownloadingUpdate => !IsDownloadingUpdate;

    partial void OnIsDownloadingUpdateChanged(bool value) => OnPropertyChanged(nameof(IsNotDownloadingUpdate));

    public bool IsBackupConfigured => !string.IsNullOrWhiteSpace(CloudBackupFolder);
    public string ToggleAutoBackupLabel => IsAutoBackupPaused ? "Resume Automatic Backup" : "Pause Automatic Backup";

    [ObservableProperty]
    private string updateRepoSlug = string.Empty;

    public void LoadMaintenanceData()
    {
        DatabasePath = AppDbContext.GetDatabasePath();

        var settings = BackupSettings.Load();
        CloudBackupFolder = settings.BackupFolder ?? string.Empty;
        MaxBackupsToKeep = Math.Clamp(settings.MaxBackupsToKeep, 1, 30);
        IsAutoBackupPaused = settings.IsAutoBackupPaused;
        UpdateRepoSlug = settings.UpdateRepoSlug;
        RefreshStatus();
    }

    [RelayCommand]
    private void BrowseCloudBackupFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select backup destination folder",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(CloudBackupFolder))
            dialog.InitialDirectory = CloudBackupFolder;

        if (dialog.ShowDialog() != true)
            return;

        CloudBackupFolder = dialog.FolderName;
        SaveSettings();
        RefreshStatus();
    }

    [RelayCommand]
    private void ClearCloudBackupFolder()
    {
        CloudBackupFolder = string.Empty;
        SaveSettings();
        RefreshStatus();
    }

    [RelayCommand]
    private void ToggleAutoBackup()
    {
        IsAutoBackupPaused = !IsAutoBackupPaused;
        OnPropertyChanged(nameof(ToggleAutoBackupLabel));
        SaveSettings();
        RefreshStatus();
    }

    [RelayCommand]
    private void BackupNow()
    {
        if (string.IsNullOrWhiteSpace(CloudBackupFolder))
        {
            MessageBox.Show("No backup folder configured. Please select a destination folder first.");
            return;
        }

        try
        {
            var dest = DatabaseMaintenanceService.AutoBackupToFolder(CloudBackupFolder, MaxBackupsToKeep, force: true);
            BackupStatusMessage = $"Last backup: {System.IO.Path.GetFileName(dest)}  ({DateTime.Now:HH:mm})";
            MessageBox.Show($"Backup saved to:\n{dest}");
        }
        catch (Exception ex)
        {
            BackupStatusMessage = $"Backup failed: {ex.Message}";
            MessageBox.Show($"Backup failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        IsCheckingUpdate = true;
        IsUpdateAvailable = false;
        UpdateStatusMessage = "Checking for updates…";
        _availableRelease = null;

        try
        {
            var release = await UpdateService.CheckForUpdateAsync();
            if (release is null)
            {
                UpdateStatusMessage = $"You are on the latest version ({CurrentVersionText}).";
            }
            else
            {
                _availableRelease = release;
                IsUpdateAvailable = true;
                UpdateStatusMessage = $"Version {release.VersionText} is available.";
            }
        }
        catch
        {
            UpdateStatusMessage = "Could not reach update server. Check your internet connection.";
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private async Task DownloadUpdate()
    {
        if (_availableRelease is null) return;

        IsDownloadingUpdate = true;
        DownloadProgress = 0;
        UpdateStatusMessage = "Downloading update…";

        try
        {
            var progress = new Progress<int>(p =>
            {
                DownloadProgress = p;
                UpdateStatusMessage = $"Downloading… {p}%";
            });

            await UpdateService.DownloadAndReplaceAsync(_availableRelease, progress, CancellationToken.None);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Download failed: {ex.Message}";
            IsDownloadingUpdate = false;
        }
    }

    [RelayCommand]
    private void RestoreDatabase()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Backup File",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt = ".db"
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirm = MessageBox.Show(
            "This will replace the current database with the selected backup. All unsaved changes will be lost.\n\nContinue?",
            "Restore Database",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            DatabaseMaintenanceService.RestoreDatabase(dialog.FileName);
            MessageBox.Show("Database restored successfully. Please restart the application.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Restore failed: {ex.Message}");
        }
    }

    partial void OnMaxBackupsToKeepChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 30);
        if (clamped != value) { MaxBackupsToKeep = clamped; return; }
        SaveSettings();
    }

    private void RefreshStatus()
    {
        OnPropertyChanged(nameof(IsBackupConfigured));
        OnPropertyChanged(nameof(ToggleAutoBackupLabel));

        if (!IsBackupConfigured)
        {
            BackupStatusMessage = string.Empty;
            return;
        }

        BackupStatusMessage = IsAutoBackupPaused
            ? "Automatic backup is paused — backups will not run on launch until resumed."
            : $"Automatic backup active → {CloudBackupFolder}";
    }

    partial void OnUpdateRepoSlugChanged(string value) => SaveSettings();

    private void SaveSettings()
    {
        new BackupSettings
        {
            BackupFolder = string.IsNullOrWhiteSpace(CloudBackupFolder) ? null : CloudBackupFolder,
            MaxBackupsToKeep = MaxBackupsToKeep,
            IsAutoBackupPaused = IsAutoBackupPaused,
            UpdateRepoSlug = string.IsNullOrWhiteSpace(UpdateRepoSlug) ? "MarianoAkiki/swm-releases" : UpdateRepoSlug
        }.Save();
    }
}
