using System.Windows;
using Site_Workforce_Manager.ViewModels;

namespace Site_Workforce_Manager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
