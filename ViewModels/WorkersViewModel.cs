using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;
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
    public ObservableCollection<WorkerRateHistory> SelectedWorkerRateHistory { get; } = new();
    public ObservableCollection<ConstructionSite> AvailableConstructionSites { get; } = new();
    public ObservableCollection<ConstructionSite> AssignedConstructionSites { get; } = new();
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
    private string newHourlyRate = string.Empty;

    [ObservableProperty]
    private DateTime? newRateEffectiveDate;

    partial void OnSelectedWorkerChanged(WorkerListItem? value)
    {
        if (value is null)
        {
            ClearWorkerForm();
            SelectedWorkerRateHistory.Clear();
            AvailableConstructionSites.Clear();
            AssignedConstructionSites.Clear();
            return;
        }

        FirstName = value.FirstName;
        LastName = value.LastName;
        LoadTradeOptions(value.TradeId);
        SelectedTradeOption = TradeOptions.FirstOrDefault(option => option.Id == value.TradeId);
        LoadRateHistory(value.Id);
        LoadConstructionSiteAssignments(value.Id);
    }

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
                TradeName = worker.Trade != null ? worker.Trade.Name : "No Trade",
                CurrentHourlyRate = worker.RateHistory
                    .Where(rate => rate.EffectiveTo == null)
                    .OrderByDescending(rate => rate.EffectiveFrom)
                    .Select(rate => rate.HourlyRate)
                    .FirstOrDefault(),
                AssignedSiteCount = worker.WorkerConstructionSites.Count,
                Status = worker.Status.ToString()
            })
            .ToList();

        Workers.Clear();

        foreach (var worker in workers)
        {
            Workers.Add(worker);
        }

        if (SelectedWorker is not null)
        {
            SelectedWorker = Workers.FirstOrDefault(worker => worker.Id == SelectedWorker.Id);
        }
    }

    [RelayCommand]
    private void AddWorker()
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName))
        {
            MessageBox.Show("Please enter first name and last name.");
            return;
        }

        if (SelectedTradeOption?.Id is not int tradeId)
        {
            MessageBox.Show("Please select a trade.");
            return;
        }

        using var context = new AppDbContext();

        var worker = new Worker
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            TradeId = tradeId,
            Status = EntityStatus.Active
        };

        context.Workers.Add(worker);
        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void EditSelectedWorker()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker to edit.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName))
        {
            MessageBox.Show("Please enter first name and last name.");
            return;
        }

        if (SelectedTradeOption?.Id is not int tradeId)
        {
            MessageBox.Show("Please select a trade.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        worker.FirstName = FirstName.Trim();
        worker.LastName = LastName.Trim();
        worker.TradeId = tradeId;

        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void DeactivateSelectedWorker()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker to deactivate.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        worker.Status = EntityStatus.Inactive;
        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void AddHourlyRate()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (!decimal.TryParse(NewHourlyRate, out var hourlyRate))
        {
            MessageBox.Show("Please enter a valid hourly rate.");
            return;
        }

        if (NewRateEffectiveDate is null)
        {
            MessageBox.Show("Please choose an effective date.");
            return;
        }

        using var context = new AppDbContext();

        var existingOpenRate = context.WorkerRateHistories
            .Where(rate => rate.WorkerId == SelectedWorker.Id && rate.EffectiveTo == null)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .FirstOrDefault();

        if (existingOpenRate is not null && existingOpenRate.EffectiveFrom < NewRateEffectiveDate.Value)
        {
            existingOpenRate.EffectiveTo = NewRateEffectiveDate.Value.AddDays(-1);
        }

        var newRate = new WorkerRateHistory
        {
            WorkerId = SelectedWorker.Id,
            HourlyRate = hourlyRate,
            EffectiveFrom = NewRateEffectiveDate.Value
        };

        context.WorkerRateHistories.Add(newRate);
        context.SaveChanges();

        NewHourlyRate = string.Empty;
        NewRateEffectiveDate = DateTime.Today;
        LoadRateHistory(SelectedWorker.Id);
    }

    [RelayCommand]
    private void AssignSite()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (SelectedAvailableConstructionSite is null)
        {
            MessageBox.Show("Please select an available construction site.");
            return;
        }

        using var context = new AppDbContext();

        var existingAssignment = context.WorkerConstructionSites
            .FirstOrDefault(item =>
                item.WorkerId == SelectedWorker.Id &&
                item.ConstructionSiteId == SelectedAvailableConstructionSite.Id);

        if (existingAssignment is not null)
        {
            MessageBox.Show("This worker is already assigned to the selected construction site.");
            return;
        }

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);
        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == SelectedAvailableConstructionSite.Id);

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
            ConstructionSiteId = SelectedAvailableConstructionSite.Id,
            AssignedDate = DateTime.Today,
            Status = EntityStatus.Active
        };

        context.WorkerConstructionSites.Add(assignment);
        context.SaveChanges();

        LoadConstructionSiteAssignments(SelectedWorker.Id);
    }

    [RelayCommand]
    private void RemoveAssignment()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (SelectedAssignedConstructionSite is null)
        {
            MessageBox.Show("Please select an assigned construction site to remove.");
            return;
        }

        using var context = new AppDbContext();

        var assignment = context.WorkerConstructionSites
            .FirstOrDefault(item =>
                item.WorkerId == SelectedWorker.Id &&
                item.ConstructionSiteId == SelectedAssignedConstructionSite.Id);

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

        SelectedAssignedConstructionSite = AssignedConstructionSites.FirstOrDefault();
        SelectedAvailableConstructionSite = AvailableConstructionSites.FirstOrDefault();
    }

    private void ClearWorkerForm()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        LoadTradeOptions();
        SelectedTradeOption = null;
    }
}
