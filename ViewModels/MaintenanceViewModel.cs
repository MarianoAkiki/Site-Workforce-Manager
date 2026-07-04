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

}
