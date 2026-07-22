using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager.ViewModels;

public partial class MaintenanceViewModel : ObservableObject
{
    [ObservableProperty]
    private string databasePath = AppDbContext.GetDatabasePath();

    [ObservableProperty]
    private string statusMessage = "Use backup before major updates to keep a copy of your data.";

    [ObservableProperty]
    private string cloudBackupFolder = string.Empty;

    [ObservableProperty]
    private int maxBackupsToKeep = 30;

    [ObservableProperty]
    private string cloudBackupStatus = string.Empty;

    public bool IsBackupConfigured => !string.IsNullOrWhiteSpace(CloudBackupFolder);

    public void LoadMaintenanceData()
    {
        DatabasePath = AppDbContext.GetDatabasePath();

        var settings = BackupSettings.Load();
        CloudBackupFolder = settings.BackupFolder ?? string.Empty;
        MaxBackupsToKeep = Math.Clamp(settings.MaxBackupsToKeep, 1, 30);
        RefreshBackupStatus();
    }

    private void RefreshBackupStatus()
    {
        OnPropertyChanged(nameof(IsBackupConfigured));
        CloudBackupStatus = IsBackupConfigured
            ? $"Auto-backup enabled → {CloudBackupFolder}"
            : string.Empty;
    }

    [RelayCommand]
    private void BackupDatabase()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Backup Database",
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                FileName = $"siteworkforcemanager-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db",
                AddExtension = true,
                DefaultExt = ".db"
            };

            if (dialog.ShowDialog() != true)
                return;

            DatabaseMaintenanceService.BackupDatabase(dialog.FileName);
            StatusMessage = $"Backup created successfully: {dialog.FileName}";
            MessageBox.Show("Database backup created successfully.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
            MessageBox.Show($"Backup failed: {ex.Message}");
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
            StatusMessage = $"Database restored from: {dialog.FileName}";
            MessageBox.Show("Database restored successfully. Please restart the application.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            MessageBox.Show($"Restore failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowseCloudBackupFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a cloud-synced folder (OneDrive, Google Drive, Dropbox, etc.)",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(CloudBackupFolder))
            dialog.InitialDirectory = CloudBackupFolder;

        if (dialog.ShowDialog() != true)
            return;

        CloudBackupFolder = dialog.FolderName;
        SaveCloudBackupSettings();
        RefreshBackupStatus();
    }

    [RelayCommand]
    private void ClearCloudBackupFolder()
    {
        CloudBackupFolder = string.Empty;
        SaveCloudBackupSettings();
        RefreshBackupStatus();
    }

    [RelayCommand]
    private void BackupNow()
    {
        if (string.IsNullOrWhiteSpace(CloudBackupFolder))
        {
            MessageBox.Show("No backup folder configured. Please select a folder first.");
            return;
        }

        try
        {
            var dest = DatabaseMaintenanceService.AutoBackupToFolder(CloudBackupFolder, MaxBackupsToKeep, force: true);
            CloudBackupStatus = $"Backup created: {System.IO.Path.GetFileName(dest)}  ({DateTime.Now:HH:mm})";
            MessageBox.Show($"Backup saved to:\n{dest}");
        }
        catch (Exception ex)
        {
            CloudBackupStatus = $"Backup failed: {ex.Message}";
            MessageBox.Show($"Backup failed: {ex.Message}");
        }
    }

    partial void OnMaxBackupsToKeepChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 30);
        if (clamped != value) { MaxBackupsToKeep = clamped; return; }
        SaveCloudBackupSettings();
    }

    private void SaveCloudBackupSettings()
    {
        var settings = new BackupSettings
        {
            BackupFolder = string.IsNullOrWhiteSpace(CloudBackupFolder) ? null : CloudBackupFolder,
            MaxBackupsToKeep = MaxBackupsToKeep
        };
        settings.Save();
    }
}
