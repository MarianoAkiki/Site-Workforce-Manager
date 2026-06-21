using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Site_Workforce_Manager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        DashboardViewModel = new DashboardViewModel();
        TradesViewModel = new TradesViewModel();
        WorkersViewModel = new WorkersViewModel();
        ConstructionSitesViewModel = new ConstructionSitesViewModel();
        WeeklyWorkEntryViewModel = new WeeklyWorkEntryViewModel();
        ReportsViewModel = new ReportsViewModel();
        PayrollViewModel = new PayrollViewModel();
        MaintenanceViewModel = new MaintenanceViewModel();
        CurrentPageKey = "Dashboard";
        CurrentViewModel = DashboardViewModel;
    }

    public DashboardViewModel DashboardViewModel { get; }
    public TradesViewModel TradesViewModel { get; }
    public WorkersViewModel WorkersViewModel { get; }
    public ConstructionSitesViewModel ConstructionSitesViewModel { get; }
    public WeeklyWorkEntryViewModel WeeklyWorkEntryViewModel { get; }
    public ReportsViewModel ReportsViewModel { get; }
    public PayrollViewModel PayrollViewModel { get; }
    public MaintenanceViewModel MaintenanceViewModel { get; }

    [ObservableProperty]
    private ObservableObject currentViewModel;

    [ObservableProperty]
    private string currentPageKey = string.Empty;

    public bool IsTradesActive => CurrentPageKey == "Trades";
    public bool IsDashboardActive => CurrentPageKey == "Dashboard";
    public bool IsWorkersActive => CurrentPageKey == "Workers";
    public bool IsConstructionSitesActive => CurrentPageKey == "ConstructionSites";
    public bool IsWeeklyWorkEntryActive => CurrentPageKey == "WeeklyWorkEntry";
    public bool IsReportsActive => CurrentPageKey == "Reports";
    public bool IsPayrollActive => CurrentPageKey == "Payroll";
    public bool IsMaintenanceActive => CurrentPageKey == "Maintenance";

    partial void OnCurrentPageKeyChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsTradesActive));
        OnPropertyChanged(nameof(IsWorkersActive));
        OnPropertyChanged(nameof(IsConstructionSitesActive));
        OnPropertyChanged(nameof(IsWeeklyWorkEntryActive));
        OnPropertyChanged(nameof(IsReportsActive));
        OnPropertyChanged(nameof(IsPayrollActive));
        OnPropertyChanged(nameof(IsMaintenanceActive));
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        DashboardViewModel.LoadDashboard();
        CurrentPageKey = "Dashboard";
        CurrentViewModel = DashboardViewModel;
    }

    [RelayCommand]
    private void ShowTrades()
    {
        TradesViewModel.ShowListPage();
        CurrentPageKey = "Trades";
        CurrentViewModel = TradesViewModel;
    }

    [RelayCommand]
    private void ShowWorkers()
    {
        WorkersViewModel.ShowListPage();
        CurrentPageKey = "Workers";
        CurrentViewModel = WorkersViewModel;
    }

    [RelayCommand]
    private void ShowConstructionSites()
    {
        ConstructionSitesViewModel.ShowListPage();
        CurrentPageKey = "ConstructionSites";
        CurrentViewModel = ConstructionSitesViewModel;
    }

    [RelayCommand]
    private void ShowWeeklyWorkEntry()
    {
        WeeklyWorkEntryViewModel.LoadWeeklyEntryPage();
        CurrentPageKey = "WeeklyWorkEntry";
        CurrentViewModel = WeeklyWorkEntryViewModel;
    }

    [RelayCommand]
    private void ShowReports()
    {
        ReportsViewModel.LoadReport();
        CurrentPageKey = "Reports";
        CurrentViewModel = ReportsViewModel;
    }

    [RelayCommand]
    private void ShowPayroll()
    {
        PayrollViewModel.LoadPayrollPage();
        CurrentPageKey = "Payroll";
        CurrentViewModel = PayrollViewModel;
    }

    [RelayCommand]
    private void ShowMaintenance()
    {
        MaintenanceViewModel.LoadMaintenanceData();
        CurrentPageKey = "Maintenance";
        CurrentViewModel = MaintenanceViewModel;
    }
}
