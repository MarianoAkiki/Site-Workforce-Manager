using System.IO;
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
}
