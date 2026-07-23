using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace Site_Workforce_Manager.Services;

public static class UpdateService
{
    private static string ApiUrl =>
        $"https://api.github.com/repos/{BackupSettings.Load().UpdateRepoSlug}/releases/latest";

    public static Version CurrentVersion =>
        Assembly.GetEntryAssembly()!.GetName().Version!;

    public static string CurrentVersionText =>
        $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public static async Task<ReleaseInfo?> CheckForUpdateAsync()
    {
        using var client = CreateClient();
        var release = await client.GetFromJsonAsync<GitHubRelease>(ApiUrl);
        if (release is null) return null;

        var tag = release.TagName.TrimStart('v');
        if (!Version.TryParse(tag, out var latest)) return null;
        if (latest <= CurrentVersion) return null;

        var asset = release.Assets.FirstOrDefault(a =>
            a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (asset is null) return null;

        return new ReleaseInfo
        {
            Version = latest,
            TagName = release.TagName,
            DownloadUrl = asset.BrowserDownloadUrl,
            FileName = asset.Name
        };
    }

    public static async Task DownloadAndReplaceAsync(
        ReleaseInfo release,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var exePath = Environment.ProcessPath!;
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(exePath) + ".new.exe");
        var batPath = Path.Combine(Path.GetTempPath(), "swm_update.bat");

        using var client = CreateClient();
        using var response = await client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file = File.Create(tempPath);

        var buffer = new byte[81920];
        long downloaded = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            downloaded += bytesRead;
            if (total > 0)
                progress.Report((int)(downloaded * 100 / total));
        }

        await file.FlushAsync(ct);
        file.Close();

        var batContent =
            $"@echo off\r\n" +
            $"ping -n 3 127.0.0.1 > nul\r\n" +
            $"move /y \"{tempPath}\" \"{exePath}\"\r\n" +
            $"start \"\" \"{exePath}\"\r\n" +
            $"del \"%~f0\"\r\n";

        await File.WriteAllTextAsync(batPath, batContent, ct);

        SqliteConnection.ClearAllPools();

        Process.Start(new ProcessStartInfo
        {
            FileName = batPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SiteWorkforceManager/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }
}

public class ReleaseInfo
{
    public Version Version { get; set; } = new();
    public string TagName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string VersionText => $"v{Version.Major}.{Version.Minor}.{Version.Build}";
}

file record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] List<GitHubAsset> Assets);

file record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
