using System.IO;
using System.Text.Json;

namespace Site_Workforce_Manager.Services;

public class BackupSettings
{
    public string? BackupFolder { get; set; }
    public int MaxBackupsToKeep { get; set; } = 30;
    public bool IsAutoBackupPaused { get; set; } = false;
    public string UpdateRepoSlug { get; set; } = "MarianoAkiki/swm-releases";

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SiteWorkforceManager",
            "backup-settings.json");

    public static BackupSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<BackupSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
