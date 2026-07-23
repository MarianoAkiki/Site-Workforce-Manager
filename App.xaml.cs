using System.IO;
using System.Windows;
using Site_Workforce_Manager.Services;
using Site_Workforce_Manager.Views;

namespace Site_Workforce_Manager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Unexpected error:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        splash.Show();

        Task.Run(() =>
        {
            DatabaseInitializer.Initialize();

            var settings = BackupSettings.Load();
            if (!settings.IsAutoBackupPaused &&
                !string.IsNullOrWhiteSpace(settings.BackupFolder) &&
                Directory.Exists(settings.BackupFolder))
            {
                try { DatabaseMaintenanceService.AutoBackupToFolder(settings.BackupFolder, settings.MaxBackupsToKeep); }
                catch { }
            }

            Task.Delay(1200).Wait();
        }).ContinueWith(t =>
        {
            if (t.Exception is not null)
            {
                MessageBox.Show($"Failed to initialize database:\n\n{t.Exception.InnerException?.Message ?? t.Exception.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            splash.FadeOutAndClose(() =>
            {
                var main = new MainWindow();
                MainWindow = main;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                main.Show();
            });
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
