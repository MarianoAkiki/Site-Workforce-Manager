using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class WorkLogsViewModel : ObservableObject
{
    private bool isUpdatingForm;
    private readonly List<LookupOption> allConstructionSiteOptions = new();

    public WorkLogsViewModel()
    {
        WorkDate = DateTime.Today;
        StartTimeText = "08:00";
        EndTimeText = "16:00";
        LoadLookupData();
        LoadWorkLogs();
    }

    public ObservableCollection<WorkLog> WorkLogs { get; } = new();
    public ObservableCollection<LookupOption> WorkerOptions { get; } = new();
    public ObservableCollection<LookupOption> ConstructionSiteOptions { get; } = new();
    public ObservableCollection<LookupOption> WorkerFilterOptions { get; } = new();
    public ObservableCollection<LookupOption> ConstructionSiteFilterOptions { get; } = new();
    public ObservableCollection<PaymentStatusOption> PaymentStatusFilterOptions { get; } = new();

    [ObservableProperty]
    private WorkLog? selectedWorkLog;

    [ObservableProperty]
    private LookupOption? selectedWorkerOption;

    [ObservableProperty]
    private LookupOption? selectedConstructionSiteOption;

    [ObservableProperty]
    private DateTime? workDate;

    [ObservableProperty]
    private string startTimeText = string.Empty;

    [ObservableProperty]
    private string endTimeText = string.Empty;

    [ObservableProperty]
    private decimal durationHours;

    [ObservableProperty]
    private decimal hourlyRateSnapshot;

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private DateTime? filterStartDate;

    [ObservableProperty]
    private DateTime? filterEndDate;

    [ObservableProperty]
    private LookupOption? selectedFilterWorkerOption;

    [ObservableProperty]
    private LookupOption? selectedFilterConstructionSiteOption;

    [ObservableProperty]
    private PaymentStatusOption? selectedFilterPaymentStatusOption;

    [ObservableProperty]
    private decimal filteredTotalHours;

    [ObservableProperty]
    private decimal filteredTotalAmount;

    [ObservableProperty]
    private string constructionSiteSelectionMessage = string.Empty;

    partial void OnSelectedWorkLogChanged(WorkLog? value)
    {
        if (value is null)
        {
            ClearForm();
            return;
        }

        isUpdatingForm = true;

        SelectedWorkerOption = WorkerOptions.FirstOrDefault(item => item.Id == value.WorkerId);
        FilterConstructionSitesForSelectedWorker(value.ConstructionSiteId);
        WorkDate = value.WorkDate;
        StartTimeText = value.StartTime.ToString(@"hh\:mm");
        EndTimeText = value.EndTime.ToString(@"hh\:mm");
        DurationHours = value.DurationHours;
        HourlyRateSnapshot = value.HourlyRateSnapshot;
        TotalAmount = value.TotalAmount;
        Notes = value.Notes;

        isUpdatingForm = false;
    }

    partial void OnSelectedWorkerOptionChanged(LookupOption? value)
    {
        if (!isUpdatingForm)
        {
            FilterConstructionSitesForSelectedWorker();
            UpdateRateAndTotals();
        }
    }

    partial void OnWorkDateChanged(DateTime? value)
    {
        if (!isUpdatingForm)
        {
            UpdateRateAndTotals();
        }
    }

    partial void OnStartTimeTextChanged(string value)
    {
        if (!isUpdatingForm)
        {
            UpdateDurationAndTotal();
        }
    }

    partial void OnEndTimeTextChanged(string value)
    {
        if (!isUpdatingForm)
        {
            UpdateDurationAndTotal();
        }
    }

    partial void OnFilterStartDateChanged(DateTime? value) => LoadWorkLogs();
    partial void OnFilterEndDateChanged(DateTime? value) => LoadWorkLogs();
    partial void OnSelectedFilterWorkerOptionChanged(LookupOption? value) => LoadWorkLogs();
    partial void OnSelectedFilterConstructionSiteOptionChanged(LookupOption? value) => LoadWorkLogs();
    partial void OnSelectedFilterPaymentStatusOptionChanged(PaymentStatusOption? value) => LoadWorkLogs();

    public void LoadWorkLogs()
    {
        using var context = new AppDbContext();

        var query = context.WorkLogs
            .AsNoTracking()
            .Include(workLog => workLog.Worker)
            .Include(workLog => workLog.ConstructionSite)
            .AsQueryable();

        if (FilterStartDate.HasValue)
        {
            var startDate = FilterStartDate.Value.Date;
            query = query.Where(workLog => workLog.WorkDate >= startDate);
        }

        if (FilterEndDate.HasValue)
        {
            var endDate = FilterEndDate.Value.Date;
            query = query.Where(workLog => workLog.WorkDate <= endDate);
        }

        if (SelectedFilterWorkerOption?.Id is int workerId)
        {
            query = query.Where(workLog => workLog.WorkerId == workerId);
        }

        if (SelectedFilterConstructionSiteOption?.Id is int siteId)
        {
            query = query.Where(workLog => workLog.ConstructionSiteId == siteId);
        }

        if (SelectedFilterPaymentStatusOption?.Value is PaymentStatus paymentStatus)
        {
            query = query.Where(workLog => workLog.PaymentStatus == paymentStatus);
        }

        var workLogs = query
            .OrderByDescending(workLog => workLog.WorkDate)
            .ThenByDescending(workLog => workLog.Id)
            .ToList();

        WorkLogs.Clear();

        foreach (var workLog in workLogs)
        {
            WorkLogs.Add(workLog);
        }

        FilteredTotalHours = Math.Round(workLogs
            .Where(workLog => workLog.PaymentStatus != PaymentStatus.Cancelled)
            .Sum(workLog => workLog.DurationHours), 2);

        FilteredTotalAmount = Math.Round(workLogs
            .Where(workLog => workLog.PaymentStatus != PaymentStatus.Cancelled)
            .Sum(workLog => workLog.TotalAmount), 2);

        if (SelectedWorkLog is not null)
        {
            SelectedWorkLog = WorkLogs.FirstOrDefault(item => item.Id == SelectedWorkLog.Id);
        }
    }

    [RelayCommand]
    private void AddWorkLog()
    {
        if (!TryBuildWorkLogValues(out var values))
        {
            return;
        }

        using var context = new AppDbContext();

        var now = DateTime.Now;
        var workLog = new WorkLog
        {
            WorkerId = values.WorkerId,
            ConstructionSiteId = values.ConstructionSiteId,
            WorkDate = values.WorkDate,
            StartTime = values.StartTime,
            EndTime = values.EndTime,
            DurationHours = values.DurationHours,
            HourlyRateSnapshot = values.HourlyRateSnapshot,
            TotalAmount = values.TotalAmount,
            PaymentStatus = PaymentStatus.Unpaid,
            Notes = values.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.WorkLogs.Add(workLog);
        context.SaveChanges();

        LoadWorkLogs();
        SelectedWorkLog = WorkLogs.FirstOrDefault(item => item.Id == workLog.Id);
    }

    [RelayCommand]
    private void EditSelectedWorkLog()
    {
        if (SelectedWorkLog is null)
        {
            MessageBox.Show("Please select a work log to edit.");
            return;
        }

        if (SelectedWorkLog.PaymentStatus == PaymentStatus.Paid)
        {
            MessageBox.Show("Paid work logs cannot be edited.");
            return;
        }

        if (SelectedWorkLog.PaymentStatus == PaymentStatus.Cancelled)
        {
            MessageBox.Show("Cancelled work logs cannot be edited.");
            return;
        }

        if (!TryBuildWorkLogValues(out var values))
        {
            return;
        }

        using var context = new AppDbContext();

        var workLog = context.WorkLogs.FirstOrDefault(item => item.Id == SelectedWorkLog.Id);

        if (workLog is null)
        {
            MessageBox.Show("The selected work log could not be found.");
            return;
        }

        if (workLog.PaymentStatus == PaymentStatus.Paid)
        {
            MessageBox.Show("Paid work logs cannot be edited.");
            return;
        }

        workLog.WorkerId = values.WorkerId;
        workLog.ConstructionSiteId = values.ConstructionSiteId;
        workLog.WorkDate = values.WorkDate;
        workLog.StartTime = values.StartTime;
        workLog.EndTime = values.EndTime;
        workLog.DurationHours = values.DurationHours;
        workLog.HourlyRateSnapshot = values.HourlyRateSnapshot;
        workLog.TotalAmount = values.TotalAmount;
        workLog.Notes = values.Notes;
        workLog.UpdatedAt = DateTime.Now;

        context.SaveChanges();

        LoadWorkLogs();
        SelectedWorkLog = WorkLogs.FirstOrDefault(item => item.Id == workLog.Id);
    }

    [RelayCommand]
    private void CancelSelectedWorkLog()
    {
        if (SelectedWorkLog is null)
        {
            MessageBox.Show("Please select a work log to cancel.");
            return;
        }

        using var context = new AppDbContext();

        var workLog = context.WorkLogs.FirstOrDefault(item => item.Id == SelectedWorkLog.Id);

        if (workLog is null)
        {
            MessageBox.Show("The selected work log could not be found.");
            return;
        }

        workLog.PaymentStatus = PaymentStatus.Cancelled;
        workLog.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        LoadWorkLogs();
        SelectedWorkLog = WorkLogs.FirstOrDefault(item => item.Id == workLog.Id);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        isUpdatingForm = true;
        FilterStartDate = null;
        FilterEndDate = null;
        SelectedFilterWorkerOption = WorkerFilterOptions.FirstOrDefault();
        SelectedFilterConstructionSiteOption = ConstructionSiteFilterOptions.FirstOrDefault();
        SelectedFilterPaymentStatusOption = PaymentStatusFilterOptions.FirstOrDefault();
        isUpdatingForm = false;

        LoadWorkLogs();
    }

    private void LoadLookupData()
    {
        using var context = new AppDbContext();

        var workers = context.Workers
            .AsNoTracking()
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new LookupOption
            {
                Id = worker.Id,
                Name = $"{worker.FirstName} {worker.LastName}"
            })
            .ToList();

        var sites = context.ConstructionSites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .Select(site => new LookupOption
            {
                Id = site.Id,
                Name = site.Name
            })
            .ToList();

        WorkerOptions.Clear();
        ConstructionSiteOptions.Clear();
        allConstructionSiteOptions.Clear();
        WorkerFilterOptions.Clear();
        ConstructionSiteFilterOptions.Clear();

        WorkerFilterOptions.Add(new LookupOption { Id = null, Name = "All Workers" });
        ConstructionSiteFilterOptions.Add(new LookupOption { Id = null, Name = "All Sites" });

        foreach (var worker in workers)
        {
            WorkerOptions.Add(worker);
            WorkerFilterOptions.Add(new LookupOption { Id = worker.Id, Name = worker.Name });
        }

        foreach (var site in sites)
        {
            allConstructionSiteOptions.Add(site);
            ConstructionSiteFilterOptions.Add(new LookupOption { Id = site.Id, Name = site.Name });
        }

        PaymentStatusFilterOptions.Clear();
        PaymentStatusFilterOptions.Add(new PaymentStatusOption { Value = null, Name = "All Statuses" });
        PaymentStatusFilterOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Unpaid, Name = "Unpaid" });
        PaymentStatusFilterOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Paid, Name = "Paid" });
        PaymentStatusFilterOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Cancelled, Name = "Cancelled" });

        SelectedFilterWorkerOption = WorkerFilterOptions.FirstOrDefault();
        SelectedFilterConstructionSiteOption = ConstructionSiteFilterOptions.FirstOrDefault();
        SelectedFilterPaymentStatusOption = PaymentStatusFilterOptions.FirstOrDefault();
        SelectedWorkerOption = WorkerOptions.FirstOrDefault();
        FilterConstructionSitesForSelectedWorker();

        UpdateRateAndTotals();
    }

    private bool TryBuildWorkLogValues(out WorkLogFormValues values)
    {
        values = new WorkLogFormValues();

        if (SelectedWorkerOption?.Id is not int workerId)
        {
            MessageBox.Show("Please select a worker.");
            return false;
        }

        if (ConstructionSiteOptions.Count == 0)
        {
            MessageBox.Show("The selected worker has no assigned construction sites. Please assign a site before creating a work log.");
            return false;
        }

        if (SelectedConstructionSiteOption?.Id is not int constructionSiteId)
        {
            MessageBox.Show("Please select a construction site.");
            return false;
        }

        if (!WorkDate.HasValue)
        {
            MessageBox.Show("Please select a work date.");
            return false;
        }

        if (!TimeSpan.TryParse(StartTimeText, out var startTime))
        {
            MessageBox.Show("Please enter a valid start time in HH:mm format.");
            return false;
        }

        if (!TimeSpan.TryParse(EndTimeText, out var endTime))
        {
            MessageBox.Show("Please enter a valid end time in HH:mm format.");
            return false;
        }

        if (endTime <= startTime)
        {
            MessageBox.Show("End time must be after start time.");
            return false;
        }

        using var context = new AppDbContext();

        var hourlyRate = GetHourlyRateForDate(context, workerId, WorkDate.Value);

        if (hourlyRate <= 0)
        {
            MessageBox.Show("No hourly rate was found for the selected worker and date.");
            return false;
        }

        var duration = Math.Round((decimal)(endTime - startTime).TotalHours, 2);
        var total = Math.Round(duration * hourlyRate, 2);

        values = new WorkLogFormValues
        {
            WorkerId = workerId,
            ConstructionSiteId = constructionSiteId,
            WorkDate = WorkDate.Value.Date,
            StartTime = startTime,
            EndTime = endTime,
            DurationHours = duration,
            HourlyRateSnapshot = hourlyRate,
            TotalAmount = total,
            Notes = Notes.Trim()
        };

        DurationHours = duration;
        HourlyRateSnapshot = hourlyRate;
        TotalAmount = total;

        return true;
    }

    private void UpdateRateAndTotals()
    {
        using var context = new AppDbContext();

        if (SelectedWorkerOption?.Id is int workerId && WorkDate.HasValue)
        {
            HourlyRateSnapshot = GetHourlyRateForDate(context, workerId, WorkDate.Value);
        }
        else
        {
            HourlyRateSnapshot = 0m;
        }

        UpdateDurationAndTotal();
    }

    private void UpdateDurationAndTotal()
    {
        if (TimeSpan.TryParse(StartTimeText, out var startTime) &&
            TimeSpan.TryParse(EndTimeText, out var endTime) &&
            endTime > startTime)
        {
            DurationHours = Math.Round((decimal)(endTime - startTime).TotalHours, 2);
            TotalAmount = Math.Round(DurationHours * HourlyRateSnapshot, 2);
            return;
        }

        DurationHours = 0m;
        TotalAmount = 0m;
    }

    private void ClearForm()
    {
        isUpdatingForm = true;
        SelectedWorkerOption = WorkerOptions.FirstOrDefault();
        FilterConstructionSitesForSelectedWorker();
        WorkDate = DateTime.Today;
        StartTimeText = "08:00";
        EndTimeText = "16:00";
        Notes = string.Empty;
        isUpdatingForm = false;

        UpdateRateAndTotals();
    }

    private static decimal GetHourlyRateForDate(AppDbContext context, int workerId, DateTime workDate)
    {
        var rate = context.WorkerRateHistories
            .Where(item => item.WorkerId == workerId &&
                           item.EffectiveFrom <= workDate &&
                           (item.EffectiveTo == null || item.EffectiveTo >= workDate))
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefault();

        return rate?.HourlyRate ?? 0m;
    }

    private void FilterConstructionSitesForSelectedWorker(int? siteIdToKeep = null)
    {
        var selectedSiteId = siteIdToKeep ?? SelectedConstructionSiteOption?.Id;

        ConstructionSiteOptions.Clear();

        if (SelectedWorkerOption?.Id is not int workerId)
        {
            ConstructionSiteSelectionMessage = "Select a worker first to see assigned construction sites.";
            SelectedConstructionSiteOption = null;
            return;
        }

        using var context = new AppDbContext();

        var assignedSiteIds = context.WorkerConstructionSites
            .AsNoTracking()
            .Where(item => item.WorkerId == workerId)
            .Select(item => item.ConstructionSiteId)
            .ToHashSet();

        var filteredSites = allConstructionSiteOptions
            .Where(site => site.Id.HasValue && assignedSiteIds.Contains(site.Id.Value))
            .OrderBy(site => site.Name)
            .ToList();

        foreach (var site in filteredSites)
        {
            ConstructionSiteOptions.Add(site);
        }

        SelectedConstructionSiteOption = ConstructionSiteOptions
            .FirstOrDefault(site => site.Id == selectedSiteId);

        if (ConstructionSiteOptions.Count == 0)
        {
            ConstructionSiteSelectionMessage = "This worker has no assigned construction sites. Assign a site on the Workers page first.";
        }
        else
        {
            ConstructionSiteSelectionMessage = string.Empty;
        }
    }

    private class WorkLogFormValues
    {
        public int WorkerId { get; set; }
        public int ConstructionSiteId { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal HourlyRateSnapshot { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
