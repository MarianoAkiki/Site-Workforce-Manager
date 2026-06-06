using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    public ReportsViewModel()
    {
        LoadLookupData();
        ApplyFilters();
    }

    public ObservableCollection<ReportRow> ReportWorkLogs { get; } = new();
    public ObservableCollection<SelectableLookupOption> WorkerOptions { get; } = new();
    public ObservableCollection<SelectableLookupOption> ConstructionSiteOptions { get; } = new();
    public ObservableCollection<PaymentStatusOption> PaymentStatusOptions { get; } = new();

    [ObservableProperty]
    private DateTime? dateFrom;

    [ObservableProperty]
    private DateTime? dateTo;

    [ObservableProperty]
    private PaymentStatusOption? selectedPaymentStatusOption;

    [ObservableProperty]
    private decimal totalHours;

    [ObservableProperty]
    private decimal totalAmount;

    public void LoadReport()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        using var context = new AppDbContext();

        var selectedWorkerIds = WorkerOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var selectedSiteIds = ConstructionSiteOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var query = context.WorkLogs
            .AsNoTracking()
            .Include(workLog => workLog.Worker)
            .Include(workLog => workLog.ConstructionSite)
            .AsQueryable();

        if (DateFrom.HasValue)
        {
            query = query.Where(workLog => workLog.WorkDate >= DateFrom.Value.Date);
        }

        if (DateTo.HasValue)
        {
            query = query.Where(workLog => workLog.WorkDate <= DateTo.Value.Date);
        }

        if (selectedWorkerIds.Count > 0)
        {
            query = query.Where(workLog => selectedWorkerIds.Contains(workLog.WorkerId));
        }

        if (selectedSiteIds.Count > 0)
        {
            query = query.Where(workLog => selectedSiteIds.Contains(workLog.ConstructionSiteId));
        }

        if (SelectedPaymentStatusOption?.Value is PaymentStatus paymentStatus)
        {
            query = query.Where(workLog => workLog.PaymentStatus == paymentStatus);
        }
        else
        {
            query = query.Where(workLog => workLog.PaymentStatus != PaymentStatus.Cancelled);
        }

        var workLogs = query
            .OrderByDescending(workLog => workLog.WorkDate)
            .ThenBy(workLog => workLog.Worker!.FirstName)
            .ThenBy(workLog => workLog.Worker!.LastName)
            .ToList();

        ReportWorkLogs.Clear();

        foreach (var workLog in workLogs)
        {
            ReportWorkLogs.Add(new ReportRow
            {
                Worker = $"{workLog.Worker?.FirstName} {workLog.Worker?.LastName}".Trim(),
                ConstructionSite = workLog.ConstructionSite?.Name ?? string.Empty,
                WorkDate = workLog.WorkDate,
                StartTime = workLog.StartTime,
                EndTime = workLog.EndTime,
                DurationHours = workLog.DurationHours,
                HourlyRate = workLog.HourlyRateSnapshot,
                TotalAmount = workLog.TotalAmount,
                PaymentStatus = workLog.PaymentStatus
            });
        }

        TotalHours = Math.Round(workLogs
            .Where(workLog => workLog.PaymentStatus != PaymentStatus.Cancelled)
            .Sum(workLog => workLog.DurationHours), 2);

        TotalAmount = Math.Round(workLogs
            .Where(workLog => workLog.PaymentStatus != PaymentStatus.Cancelled)
            .Sum(workLog => workLog.TotalAmount), 2);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        DateFrom = null;
        DateTo = null;
        SelectedPaymentStatusOption = PaymentStatusOptions.FirstOrDefault();

        foreach (var option in WorkerOptions)
        {
            option.IsSelected = false;
        }

        foreach (var option in ConstructionSiteOptions)
        {
            option.IsSelected = false;
        }

        ApplyFilters();
    }

    private void LoadLookupData()
    {
        using var context = new AppDbContext();

        WorkerOptions.Clear();
        ConstructionSiteOptions.Clear();
        PaymentStatusOptions.Clear();

        var workers = context.Workers
            .AsNoTracking()
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new SelectableLookupOption
            {
                Id = worker.Id,
                Name = $"{worker.FirstName} {worker.LastName}"
            })
            .ToList();

        var sites = context.ConstructionSites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .Select(site => new SelectableLookupOption
            {
                Id = site.Id,
                Name = site.Name
            })
            .ToList();

        foreach (var worker in workers)
        {
            WorkerOptions.Add(worker);
        }

        foreach (var site in sites)
        {
            ConstructionSiteOptions.Add(site);
        }

        PaymentStatusOptions.Add(new PaymentStatusOption { Value = null, Name = "All" });
        PaymentStatusOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Paid, Name = "Paid" });
        PaymentStatusOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Unpaid, Name = "Unpaid" });
        PaymentStatusOptions.Add(new PaymentStatusOption { Value = PaymentStatus.Cancelled, Name = "Cancelled" });

        SelectedPaymentStatusOption = PaymentStatusOptions.FirstOrDefault();
    }

    public class ReportRow
    {
        public string Worker { get; set; } = string.Empty;
        public string ConstructionSite { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
