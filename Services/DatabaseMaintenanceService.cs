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
}
