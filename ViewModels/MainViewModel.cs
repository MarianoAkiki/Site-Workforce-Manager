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
        WorkLogsViewModel = new WorkLogsViewModel();
        ReportsViewModel = new ReportsViewModel();
        PayrollViewModel = new PayrollViewModel();
        WorkerBalancesViewModel = new WorkerBalancesViewModel();
        MaintenanceViewModel = new MaintenanceViewModel();
        CurrentPageKey = "Dashboard";
        CurrentViewModel = DashboardViewModel;
    }

    public DashboardViewModel DashboardViewModel { get; }
    public TradesViewModel TradesViewModel { get; }
    public WorkersViewModel WorkersViewModel { get; }
    public ConstructionSitesViewModel ConstructionSitesViewModel { get; }
    public WorkLogsViewModel WorkLogsViewModel { get; }
    public ReportsViewModel ReportsViewModel { get; }
    public PayrollViewModel PayrollViewModel { get; }
    public WorkerBalancesViewModel WorkerBalancesViewModel { get; }
    public MaintenanceViewModel MaintenanceViewModel { get; }

    [ObservableProperty]
    private ObservableObject currentViewModel;

    [ObservableProperty]
    private string currentPageKey = string.Empty;

    public bool IsTradesActive => CurrentPageKey == "Trades";
    public bool IsDashboardActive => CurrentPageKey == "Dashboard";
    public bool IsWorkersActive => CurrentPageKey == "Workers";
    public bool IsConstructionSitesActive => CurrentPageKey == "ConstructionSites";
    public bool IsWorkLogsActive => CurrentPageKey == "WorkLogs";
    public bool IsReportsActive => CurrentPageKey == "Reports";
    public bool IsPayrollActive => CurrentPageKey == "Payroll";
    public bool IsWorkerBalancesActive => CurrentPageKey == "WorkerBalances";
    public bool IsMaintenanceActive => CurrentPageKey == "Maintenance";

    partial void OnCurrentPageKeyChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsTradesActive));
        OnPropertyChanged(nameof(IsWorkersActive));
        OnPropertyChanged(nameof(IsConstructionSitesActive));
        OnPropertyChanged(nameof(IsWorkLogsActive));
        OnPropertyChanged(nameof(IsReportsActive));
        OnPropertyChanged(nameof(IsPayrollActive));
        OnPropertyChanged(nameof(IsWorkerBalancesActive));
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
        TradesViewModel.LoadTrades();
        CurrentPageKey = "Trades";
        CurrentViewModel = TradesViewModel;
    }

    [RelayCommand]
    private void ShowWorkers()
    {
        WorkersViewModel.LoadWorkers();
        CurrentPageKey = "Workers";
        CurrentViewModel = WorkersViewModel;
    }

    [RelayCommand]
    private void ShowConstructionSites()
    {
        ConstructionSitesViewModel.LoadConstructionSites();
        CurrentPageKey = "ConstructionSites";
        CurrentViewModel = ConstructionSitesViewModel;
    }

    [RelayCommand]
    private void ShowWorkLogs()
    {
        WorkLogsViewModel.LoadWorkLogs();
        CurrentPageKey = "WorkLogs";
        CurrentViewModel = WorkLogsViewModel;
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
    private void ShowWorkerBalances()
    {
        WorkerBalancesViewModel.LoadWorkerBalances();
        CurrentPageKey = "WorkerBalances";
        CurrentViewModel = WorkerBalancesViewModel;
    }

    [RelayCommand]
    private void ShowMaintenance()
    {
        MaintenanceViewModel.LoadMaintenanceData();
        CurrentPageKey = "Maintenance";
        CurrentViewModel = MaintenanceViewModel;
    }
}
