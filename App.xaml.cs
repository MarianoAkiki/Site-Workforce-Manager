using System.Windows;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DatabaseInitializer.Initialize();
        base.OnStartup(e);
    }
}
