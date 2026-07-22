using System.IO;
using Microsoft.Data.Sqlite;
using Site_Workforce_Manager.Data;

namespace Site_Workforce_Manager.Services;

public static class DatabaseMaintenanceService
{
    public static void BackupDatabase(string destinationFilePath)
    {
        var databasePath = AppDbContext.GetDatabasePath();

        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("The database file could not be found.", databasePath);
        }

        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);

        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(databasePath, destinationFilePath, overwrite: true);
    }

    public static void RestoreDatabase(string sourceFilePath)
    {
        var databasePath = AppDbContext.GetDatabasePath();

        SqliteConnection.ClearAllPools();
        File.Copy(sourceFilePath, databasePath, overwrite: true);
    }

    public static string? AutoBackupToFolder(string backupFolder, int maxBackupsToKeep, bool force = false)
    {
        var databasePath = AppDbContext.GetDatabasePath();

        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Database file not found.", databasePath);

        Directory.CreateDirectory(backupFolder);

        if (!force)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var alreadyBackedUpToday = Directory
                .GetFiles(backupFolder, $"siteworkforcemanager_{today}_*.db")
                .Length > 0;

            if (alreadyBackedUpToday)
                return null;
        }

        var destPath = Path.Combine(backupFolder,
            $"siteworkforcemanager_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        File.Copy(databasePath, destPath, overwrite: true);

        var old = Directory.GetFiles(backupFolder, "siteworkforcemanager_*.db")
            .OrderByDescending(f => f)
            .Skip(maxBackupsToKeep);

        foreach (var f in old)
        {
            try { File.Delete(f); } catch { }
        }

        return destPath;
    }
}
