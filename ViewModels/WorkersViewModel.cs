using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;
using System.Windows;

namespace Site_Workforce_Manager.ViewModels;

public partial class WorkersViewModel : ObservableObject
{
    public WorkersViewModel()
    {
        NewRateEffectiveDate = DateTime.Today;
        LoadTradeOptions();
        LoadWorkers();
    }

    public ObservableCollection<WorkerListItem> Workers { get; } = new();
    public ObservableCollection<WorkerListItem> FilteredWorkers { get; } = new();
    public PagedList<WorkerListItem> WorkersPage { get; } = new(25);
    public ObservableCollection<WorkerRateHistory> SelectedWorkerRateHistory { get; } = new();
    public ObservableCollection<ConstructionSite> AvailableConstructionSites { get; } = new();
    public ObservableCollection<ConstructionSite> AssignedConstructionSites { get; } = new();
    public ObservableCollection<ConstructionSite> FilteredAvailableConstructionSites { get; } = new();
    public ObservableCollection<ConstructionSite> FilteredAssignedConstructionSites { get; } = new();
    public ObservableCollection<LookupOption> TradeOptions { get; } = new();

    [ObservableProperty]
    private WorkerListItem? selectedWorker;

    [ObservableProperty]
    private ConstructionSite? selectedAvailableConstructionSite;

    [ObservableProperty]
    private ConstructionSite? selectedAssignedConstructionSite;

    [ObservableProperty]
    private LookupOption? selectedTradeOption;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private DateTime startedAt = DateTime.Today;

    [ObservableProperty]
    private string newDailyRate = string.Empty;

    [ObservableProperty]
    private DateTime? newRateEffectiveDate;

    [ObservableProperty]
    private WorkerRateHistory? editingRate;

    [ObservableProperty]
    private string editingRateValue = string.Empty;

    public bool IsEditingRate => EditingRate is not null;

    partial void OnEditingRateChanged(WorkerRateHistory? value)
    {
        OnPropertyChanged(nameof(IsEditingRate));
    }

    [ObservableProperty]
    private string availableSiteSearchText = string.Empty;

    [ObservableProperty]
    private string assignedSiteSearchText = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isWorkerFormVisible;

    [ObservableProperty]
    private bool canManageWorkerDetails;

    [ObservableProperty]
    private bool showActiveWorkers = true;

    [ObservableProperty]
    private string formTitle = "Add Worker";

    [ObservableProperty]
    private string formDescription = "Create a worker profile and manage workforce details.";

    [ObservableProperty]
    private string saveButtonText = "Save Worker";

    private int? editingWorkerId;

    partial void OnSelectedWorkerChanged(WorkerListItem? value)
    {
        if (value is null)
        {
            ClearWorkerForm();
            SelectedWorkerRateHistory.Clear();
            AvailableConstructionSites.Clear();
            AssignedConstructionSites.Clear();
            FilteredAvailableConstructionSites.Clear();
            FilteredAssignedConstructionSites.Clear();
            return;
        }

        FirstName = value.FirstName;
        LastName = value.LastName;
        StartedAt = value.StartedAt;
        LoadTradeOptions(value.TradeId);
        SelectedTradeOption = TradeOptions.FirstOrDefault(option => option.Id == value.TradeId);
        LoadRateHistory(value.Id);
        LoadConstructionSiteAssignments(value.Id);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyWorkerFilter();
    }

    partial void OnAvailableSiteSearchTextChanged(string value)
    {
        ApplyConstructionSiteAssignmentFilters();
    }

    partial void OnAssignedSiteSearchTextChanged(string value)
    {
        ApplyConstructionSiteAssignmentFilters();
    }

    partial void OnShowActiveWorkersChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusFilterButtonText));
        OnPropertyChanged(nameof(StatusFilterLabel));
        ApplyWorkerFilter();
    }

    public string StatusFilterButtonText => ShowActiveWorkers ? "Show Inactive" : "Show Active";
    public string StatusFilterLabel => ShowActiveWorkers ? "Active workers" : "Inactive workers";

    public void LoadWorkers()
    {
        using var context = new AppDbContext();

        var workers = context.Workers
            .AsNoTracking()
            .Include(worker => worker.RateHistory)
            .Include(worker => worker.WorkerConstructionSites)
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new WorkerListItem
            {
                Id = worker.Id,
                FirstName = worker.FirstName,
                LastName = worker.LastName,
                WorkerName = worker.FirstName + " " + worker.LastName,
                TradeId = worker.TradeId,
                TradeName = worker.Trade != null ? worker.Trade.Name : "No Category",
                CurrentDailyRate = worker.RateHistory
                    .Where(rate => rate.EffectiveFrom <= DateTime.Today &&
                                   (rate.EffectiveTo == null || rate.EffectiveTo >= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveFrom)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => rate.DailyRate)
                    .FirstOrDefault(),
                AssignedSiteCount = worker.WorkerConstructionSites.Count,
                Status = worker.Status.ToString(),
                IsActive = worker.Status == EntityStatus.Active,
                DeactivatedAt = worker.DeactivatedAt,
                StartedAt = worker.StartedAt
            })
            .ToList();

        Workers.Clear();

        foreach (var worker in workers)
        {
            Workers.Add(worker);
        }

        ApplyWorkerFilter();

        if (SelectedWorker is not null)
        {
            SelectedWorker = FilteredWorkers.FirstOrDefault(worker => worker.Id == SelectedWorker.Id);
        }
    }

    public void ShowListPage()
    {
        IsWorkerFormVisible = false;
        CanManageWorkerDetails = false;
        editingWorkerId = null;
        SelectedWorker = null;
        SelectedWorkerRateHistory.Clear();
        AvailableConstructionSites.Clear();
        AssignedConstructionSites.Clear();
        FilteredAvailableConstructionSites.Clear();
        FilteredAssignedConstructionSites.Clear();
        ClearWorkerForm();
        LoadWorkers();
    }

    [RelayCommand]
    private void ToggleStatusFilter()
    {
        ShowActiveWorkers = !ShowActiveWorkers;
        SelectedWorker = null;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ClearAvailableSiteSearch()
    {
        AvailableSiteSearchText = string.Empty;
    }

    [RelayCommand]
    private void ClearAssignedSiteSearch()
    {
        AssignedSiteSearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenAddWorkerForm()
    {
        editingWorkerId = null;
        SelectedWorker = null;
        StartedAt = DateTime.Today;
        ClearWorkerForm();
        SelectedWorkerRateHistory.Clear();
        AvailableConstructionSites.Clear();
        AssignedConstructionSites.Clear();
        FilteredAvailableConstructionSites.Clear();
        FilteredAssignedConstructionSites.Clear();
        FormTitle = "Add Worker";
        FormDescription = "Create a worker profile. Hourly rates and site assignments can be added after saving.";
        SaveButtonText = "Save Worker";
        CanManageWorkerDetails = false;
        IsWorkerFormVisible = true;
    }

    [RelayCommand]
    private void OpenEditWorkerForm(WorkerListItem? worker)
    {
        if (worker is null)
        {
            MessageBox.Show("Please select a worker to edit.");
            return;
        }

        editingWorkerId = worker.Id;
        SelectedWorker = worker;
        FormTitle = "Worker Information";
        FormDescription = "Update worker details, daily rates, and construction site assignments.";
        SaveButtonText = "Save Changes";
        CanManageWorkerDetails = true;
        IsWorkerFormVisible = true;
    }

    [RelayCommand]
    private void CancelWorkerForm()
    {
        IsWorkerFormVisible = false;
        CanManageWorkerDetails = false;
        editingWorkerId = null;
        ClearWorkerForm();
        SelectedWorker = null;
        SelectedWorkerRateHistory.Clear();
        AvailableConstructionSites.Clear();
        AssignedConstructionSites.Clear();
        FilteredAvailableConstructionSites.Clear();
        FilteredAssignedConstructionSites.Clear();
    }

    [RelayCommand]
    private void SaveWorker()
    {
        if (editingWorkerId.HasValue)
        {
            UpdateWorker(editingWorkerId.Value);
            return;
        }

        CreateWorker();
    }

    private void CreateWorker()
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName))
        {
            MessageBox.Show("Please enter first name and last name.");
            return;
        }

        if (SelectedTradeOption?.Id is not int tradeId)
        {
            MessageBox.Show("Please select a category.");
            return;
        }

        using var context = new AppDbContext();

        var worker = new Worker
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            TradeId = tradeId,
            Status = EntityStatus.Active,
            StartedAt = StartedAt.Date
        };

        context.Workers.Add(worker);
        context.SaveChanges();

        ShowActiveWorkers = true;
        LoadWorkers();
        SelectedWorker = FilteredWorkers.FirstOrDefault(item => item.Id == worker.Id);
        editingWorkerId = worker.Id;
        FormTitle = "Worker Information";
        FormDescription = "Update worker details, daily rates, and construction site assignments.";
        SaveButtonText = "Save Changes";
        CanManageWorkerDetails = true;
        ToastNotificationService.ShowSuccess("Worker added successfully. You can now add daily rates and assign construction sites.");
    }

    private void UpdateWorker(int workerId)
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName))
        {
            MessageBox.Show("Please enter first name and last name.");
            return;
        }

        if (SelectedTradeOption?.Id is not int tradeId)
        {
            MessageBox.Show("Please select a category.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == workerId);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        worker.FirstName = FirstName.Trim();
        worker.LastName = LastName.Trim();
        worker.TradeId = tradeId;
        worker.StartedAt = StartedAt.Date;

        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = FilteredWorkers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void ToggleWorkerStatus(WorkerListItem? selectedWorker)
    {
        if (selectedWorker is null)
        {
            MessageBox.Show("Please select a worker to update.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == selectedWorker.Id);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        var isDeactivating = worker.Status == EntityStatus.Active;
        var confirmed = ConfirmationDialogService.Show(
            isDeactivating ? "Deactivate worker?" : "Activate worker?",
            isDeactivating
                ? $"Are you sure you want to deactivate \"{worker.FirstName} {worker.LastName}\"? They will remain visible in history, but should not be used for new work."
                : $"Are you sure you want to activate \"{worker.FirstName} {worker.LastName}\"? They will become available again.",
            isDeactivating ? "Deactivate" : "Activate",
            "Cancel",
            isDeactivating);

        if (!confirmed)
        {
            LoadWorkers();
            return;
        }

        worker.Status = isDeactivating ? EntityStatus.Inactive : EntityStatus.Active;
        worker.DeactivatedAt = isDeactivating ? DateTime.Now : null;
        context.SaveChanges();

        var updatedWorkerId = worker.Id;
        LoadWorkers();
        SelectedWorker = FilteredWorkers.FirstOrDefault(item => item.Id == updatedWorkerId);
    }

    [RelayCommand]
    private void AddDailyRate()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (!decimal.TryParse(NewDailyRate, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var dailyRate))
        {
            MessageBox.Show("Please enter a valid daily rate.");
            return;
        }

        if (dailyRate <= 0)
        {
            MessageBox.Show("Daily rate must be greater than zero.");
            return;
        }

        if (NewRateEffectiveDate is null)
        {
            MessageBox.Show("Please choose an effective date.");
            return;
        }

        using var context = new AppDbContext();

        var workerId = SelectedWorker.Id;
        var effectiveDate = NewRateEffectiveDate.Value.Date;
        var rateHistory = context.WorkerRateHistories
            .Where(rate => rate.WorkerId == workerId)
            .OrderBy(rate => rate.EffectiveFrom)
            .ThenBy(rate => rate.Id)
            .ToList();

        var sameDateRate = rateHistory
            .FirstOrDefault(rate => rate.EffectiveFrom.Date == effectiveDate);

        if (sameDateRate is not null)
        {
            MessageBox.Show("A rate already exists for this worker on the selected effective date. Please choose a different date.");
            return;
        }

        var previousRate = rateHistory
            .Where(rate => rate.EffectiveFrom.Date < effectiveDate)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ThenByDescending(rate => rate.Id)
            .FirstOrDefault();

        var nextRate = rateHistory
            .Where(rate => rate.EffectiveFrom.Date > effectiveDate)
            .OrderBy(rate => rate.EffectiveFrom)
            .ThenBy(rate => rate.Id)
            .FirstOrDefault();

        var newRateEffectiveTo = nextRate?.EffectiveFrom.Date.AddDays(-1);

        if (previousRate is not null)
        {
            previousRate.EffectiveTo = effectiveDate.AddDays(-1);
        }

        var newRate = new WorkerRateHistory
        {
            WorkerId = workerId,
            DailyRate = dailyRate,
            EffectiveFrom = effectiveDate,
            EffectiveTo = newRateEffectiveTo
        };

        context.WorkerRateHistories.Add(newRate);

        // Recalculate existing logs that fall within this new rate's effective range
        var effectiveTo = newRateEffectiveTo ?? DateTime.MaxValue.Date;
        var affectedLogs = context.WorkLogs
            .Where(log => log.WorkerId == workerId && log.WorkDate >= effectiveDate && log.WorkDate <= effectiveTo)
            .ToList();

        foreach (var log in affectedLogs)
        {
            log.DailyRateSnapshot = dailyRate;
            log.TotalAmount = Math.Round(dailyRate / 8m * log.DurationHours, 2);
        }

        context.SaveChanges();

        NewDailyRate = string.Empty;
        NewRateEffectiveDate = DateTime.Today;
        LoadRateHistory(workerId);
        LoadWorkers();
        SelectedWorker = FilteredWorkers.FirstOrDefault(item => item.Id == workerId);
    }

    [RelayCommand]
    private void OpenEditRate(WorkerRateHistory? rate)
    {
        if (rate is null) return;
        EditingRate = rate;
        EditingRateValue = rate.DailyRate.ToString("0.##");
    }

    [RelayCommand]
    private void CancelEditRate()
    {
        EditingRate = null;
        EditingRateValue = string.Empty;
    }

    [RelayCommand]
    private void SaveEditedRate()
    {
        if (EditingRate is null) return;

        if (!decimal.TryParse(EditingRateValue, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var newRate) || newRate <= 0)
        {
            MessageBox.Show("Please enter a valid daily rate greater than zero.");
            return;
        }

        var oldRate = EditingRate.DailyRate;

        if (oldRate == newRate)
        {
            EditingRate = null;
            EditingRateValue = string.Empty;
            return;
        }

        using var context = new AppDbContext();

        var rateRecord = context.WorkerRateHistories.FirstOrDefault(r => r.Id == EditingRate.Id);
        if (rateRecord is null) return;

        rateRecord.DailyRate = newRate;

        var effectiveFrom = EditingRate.EffectiveFrom.Date;
        var effectiveTo = EditingRate.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

        var affectedLogs = context.WorkLogs
            .Where(log =>
                log.WorkerId == EditingRate.WorkerId &&
                log.WorkDate >= effectiveFrom &&
                log.WorkDate <= effectiveTo)
            .ToList();

        foreach (var log in affectedLogs)
        {
            log.DailyRateSnapshot = newRate;
            log.TotalAmount = Math.Round(newRate / 8m * log.DurationHours, 2);
        }

        context.SaveChanges();

        var workerId = EditingRate.WorkerId;
        EditingRate = null;
        EditingRateValue = string.Empty;

        LoadRateHistory(workerId);
        LoadWorkers();
        SelectedWorker = FilteredWorkers.FirstOrDefault(w => w.Id == workerId);

        ToastNotificationService.ShowSuccess($"Rate updated. {affectedLogs.Count} work log(s) recalculated.");
    }

    [RelayCommand]
    private void AssignSite(ConstructionSite? siteToAssign)
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (siteToAssign is null)
        {
            MessageBox.Show("Please select an available construction site.");
            return;
        }

        using var context = new AppDbContext();

        var existingAssignment = context.WorkerConstructionSites
            .FirstOrDefault(item =>
                item.WorkerId == SelectedWorker.Id &&
                item.ConstructionSiteId == siteToAssign.Id);

        if (existingAssignment is not null)
        {
            MessageBox.Show("This worker is already assigned to the selected construction site.");
            return;
        }

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);
        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == siteToAssign.Id);

        if (worker is null || worker.Status != EntityStatus.Active)
        {
            MessageBox.Show("Only active workers can be assigned to construction sites.");
            return;
        }

        if (site is null || site.Status != EntityStatus.Active)
        {
            MessageBox.Show("Only active construction sites can be assigned.");
            return;
        }

        var assignment = new WorkerConstructionSite
        {
            WorkerId = SelectedWorker.Id,
            ConstructionSiteId = siteToAssign.Id,
            AssignedDate = DateTime.Today,
            Status = EntityStatus.Active
        };

        context.WorkerConstructionSites.Add(assignment);
        context.SaveChanges();

        LoadConstructionSiteAssignments(SelectedWorker.Id);
    }

    [RelayCommand]
    private void RemoveAssignment(ConstructionSite? siteToRemove)
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (siteToRemove is null)
        {
            MessageBox.Show("Please select an assigned construction site to remove.");
            return;
        }

        using var context = new AppDbContext();

        var assignment = context.WorkerConstructionSites
            .FirstOrDefault(item =>
                item.WorkerId == SelectedWorker.Id &&
                item.ConstructionSiteId == siteToRemove.Id);

        if (assignment is null)
        {
            MessageBox.Show("The selected assignment could not be found.");
            return;
        }

        context.WorkerConstructionSites.Remove(assignment);
        context.SaveChanges();

        LoadConstructionSiteAssignments(SelectedWorker.Id);
    }

    private void LoadRateHistory(int workerId)
    {
        using var context = new AppDbContext();

        var rateHistory = context.WorkerRateHistories
            .AsNoTracking()
            .Where(rate => rate.WorkerId == workerId)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ToList();

        SelectedWorkerRateHistory.Clear();

        foreach (var rate in rateHistory)
        {
            SelectedWorkerRateHistory.Add(rate);
        }
    }

    private void LoadTradeOptions(int? includeTradeId = null)
    {
        using var context = new AppDbContext();

        var activeTrades = context.Trades
            .AsNoTracking()
            .Where(trade => trade.IsActive)
            .OrderBy(trade => trade.Name)
            .Select(trade => new LookupOption
            {
                Id = trade.Id,
                Name = trade.Name
            })
            .ToList();

        LookupOption? inactiveSelectedTrade = null;

        if (includeTradeId.HasValue)
        {
            inactiveSelectedTrade = context.Trades
                .AsNoTracking()
                .Where(trade => trade.Id == includeTradeId.Value && !trade.IsActive)
                .Select(trade => new LookupOption
                {
                    Id = trade.Id,
                    Name = trade.Name
                })
                .FirstOrDefault();
        }

        TradeOptions.Clear();

        foreach (var trade in activeTrades)
        {
            TradeOptions.Add(trade);
        }

        if (inactiveSelectedTrade is not null &&
            TradeOptions.All(option => option.Id != inactiveSelectedTrade.Id))
        {
            TradeOptions.Add(inactiveSelectedTrade);
        }
    }

    private void LoadConstructionSiteAssignments(int workerId)
    {
        using var context = new AppDbContext();

        var assignedSiteIds = context.WorkerConstructionSites
            .AsNoTracking()
            .Where(item => item.WorkerId == workerId)
            .Select(item => item.ConstructionSiteId)
            .ToList();

        var assignedSites = context.ConstructionSites
            .AsNoTracking()
            .Where(site => assignedSiteIds.Contains(site.Id))
            .OrderBy(site => site.Name)
            .ToList();

        var availableSites = context.ConstructionSites
            .AsNoTracking()
            .Where(site => !assignedSiteIds.Contains(site.Id) && site.Status == EntityStatus.Active)
            .OrderBy(site => site.Name)
            .ToList();

        AssignedConstructionSites.Clear();
        AvailableConstructionSites.Clear();

        foreach (var site in assignedSites)
        {
            AssignedConstructionSites.Add(site);
        }

        foreach (var site in availableSites)
        {
            AvailableConstructionSites.Add(site);
        }

        ApplyConstructionSiteAssignmentFilters();
        SelectedAssignedConstructionSite = AssignedConstructionSites.FirstOrDefault();
        SelectedAvailableConstructionSite = AvailableConstructionSites.FirstOrDefault();
    }

    private void ClearWorkerForm()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        StartedAt = DateTime.Today;
        AvailableSiteSearchText = string.Empty;
        AssignedSiteSearchText = string.Empty;
        LoadTradeOptions();
        SelectedTradeOption = null;
    }

    private void ApplyWorkerFilter()
    {
        var search = SearchText.Trim();
        var filteredWorkers = Workers
            .Where(worker => worker.IsActive == ShowActiveWorkers)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredWorkers = filteredWorkers.Where(worker =>
                worker.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                worker.WorkerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                worker.TradeName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredWorkers.Clear();

        foreach (var worker in filteredWorkers.OrderBy(worker => worker.WorkerName))
        {
            FilteredWorkers.Add(worker);
        }

        WorkersPage.SetSource(FilteredWorkers);
    }

    private void ApplyConstructionSiteAssignmentFilters()
    {
        var availableSearch = AvailableSiteSearchText.Trim();
        var assignedSearch = AssignedSiteSearchText.Trim();

        var availableSites = AvailableConstructionSites.AsEnumerable();
        var assignedSites = AssignedConstructionSites.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(availableSearch))
        {
            availableSites = availableSites.Where(site =>
                site.Name.Contains(availableSearch, StringComparison.OrdinalIgnoreCase) ||
                site.Location.Contains(availableSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(assignedSearch))
        {
            assignedSites = assignedSites.Where(site =>
                site.Name.Contains(assignedSearch, StringComparison.OrdinalIgnoreCase) ||
                site.Location.Contains(assignedSearch, StringComparison.OrdinalIgnoreCase));
        }

        FilteredAvailableConstructionSites.Clear();
        FilteredAssignedConstructionSites.Clear();

        foreach (var site in availableSites.OrderBy(site => site.Name))
        {
            FilteredAvailableConstructionSites.Add(site);
        }

        foreach (var site in assignedSites.OrderBy(site => site.Name))
        {
            FilteredAssignedConstructionSites.Add(site);
        }
    }
}
