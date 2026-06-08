using System.IO;
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
    private string statusMessage = "Use backup before major updates, and restore only from trusted SQLite backup files.";

    public void LoadMaintenanceData()
    {
        DatabasePath = AppDbContext.GetDatabasePath();
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
            {
                return;
            }

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
        try
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Restore Database",
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Restore the database from this backup?\n\n{openDialog.FileName}\n\nThis will replace the current database.",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            DatabaseMaintenanceService.RestoreDatabase(openDialog.FileName);
            DatabaseInitializer.Initialize();

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ReloadApplicationData();
            }

            DatabasePath = AppDbContext.GetDatabasePath();
            StatusMessage = $"Database restored successfully from: {Path.GetFileName(openDialog.FileName)}";
            MessageBox.Show("Database restored successfully.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            MessageBox.Show($"Restore failed: {ex.Message}");
        }
    }
}
