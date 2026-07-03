using System.Windows;
using Site_Workforce_Manager.Services;
using Site_Workforce_Manager.Views;

namespace Site_Workforce_Manager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        splash.Show();

        Task.Run(() =>
        {
            DatabaseInitializer.Initialize();
            Task.Delay(1200).Wait();
        }).ContinueWith(_ =>
        {
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
