using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Site_Workforce_Manager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        WorkersViewModel = new WorkersViewModel();
        ConstructionSitesViewModel = new ConstructionSitesViewModel();
        WorkLogsViewModel = new WorkLogsViewModel();
        ReportsViewModel = new ReportsViewModel();
        CurrentPageKey = "Workers";
        CurrentViewModel = WorkersViewModel;
    }

    public WorkersViewModel WorkersViewModel { get; }
    public ConstructionSitesViewModel ConstructionSitesViewModel { get; }
    public WorkLogsViewModel WorkLogsViewModel { get; }
    public ReportsViewModel ReportsViewModel { get; }

    [ObservableProperty]
    private ObservableObject currentViewModel;

    [ObservableProperty]
    private string currentPageKey = string.Empty;

    public bool IsWorkersActive => CurrentPageKey == "Workers";
    public bool IsConstructionSitesActive => CurrentPageKey == "ConstructionSites";
    public bool IsWorkLogsActive => CurrentPageKey == "WorkLogs";
    public bool IsReportsActive => CurrentPageKey == "Reports";

    partial void OnCurrentPageKeyChanged(string value)
    {
        OnPropertyChanged(nameof(IsWorkersActive));
        OnPropertyChanged(nameof(IsConstructionSitesActive));
        OnPropertyChanged(nameof(IsWorkLogsActive));
        OnPropertyChanged(nameof(IsReportsActive));
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
}
