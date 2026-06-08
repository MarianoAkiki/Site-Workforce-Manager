using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class WorkerBalancesViewModel : ObservableObject
{
    private bool isInitializing;

    public WorkerBalancesViewModel()
    {
        isInitializing = true;
        LoadLookupData();
        isInitializing = false;
        LoadBalances();
    }

    public ObservableCollection<SelectableLookupOption> WorkerOptions { get; } = new();
    public ObservableCollection<SelectableLookupOption> TradeOptions { get; } = new();
    public ObservableCollection<BalanceStatusOption> BalanceStatusOptions { get; } = new();
    public ObservableCollection<WorkerBalanceRow> WorkerBalances { get; } = new();
    public ObservableCollection<WorkerSlipHistoryRow> SelectedWorkerPayrollSlips { get; } = new();
    public ObservableCollection<WorkerPaymentRow> SelectedWorkerPayments { get; } = new();

    [ObservableProperty]
    private DateTime? dateFrom;

    [ObservableProperty]
    private DateTime? dateTo;

    [ObservableProperty]
    private BalanceStatusOption? selectedBalanceStatusOption;

    [ObservableProperty]
    private WorkerBalanceRow? selectedWorkerBalance;

    [ObservableProperty]
    private decimal totalEarnedSummary;

    [ObservableProperty]
    private decimal totalPaidSummary;

    [ObservableProperty]
    private decimal totalRemainingBalanceSummary;

    [ObservableProperty]
    private int workersWithBalanceSummary;

    [ObservableProperty]
    private string selectedWorkerName = "Select a worker balance to view details.";

    partial void OnDateFromChanged(DateTime? value)
    {
        if (!isInitializing)
        {
            LoadBalances();
        }
    }

    partial void OnDateToChanged(DateTime? value)
    {
        if (!isInitializing)
        {
            LoadBalances();
        }
    }

    partial void OnSelectedBalanceStatusOptionChanged(BalanceStatusOption? value)
    {
        if (!isInitializing)
        {
            LoadBalances();
        }
    }

    partial void OnSelectedWorkerBalanceChanged(WorkerBalanceRow? value)
    {
        LoadSelectedWorkerDetails();
    }

    public void LoadWorkerBalances()
    {
        LoadLookupData();
        LoadBalances();
    }

    [RelayCommand]
    private void LoadBalances()
    {
        using var context = new AppDbContext();

        var selectedWorkerIds = WorkerOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var selectedTradeIds = TradeOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var slipsQuery = context.PayrollSlips
            .AsNoTracking()
            .Include(slip => slip.Worker)
            .ThenInclude(worker => worker!.Trade)
            .Include(slip => slip.PayrollPayments)
            .AsQueryable();

        if (selectedWorkerIds.Count > 0)
        {
            slipsQuery = slipsQuery.Where(slip => selectedWorkerIds.Contains(slip.WorkerId));
        }

        if (selectedTradeIds.Count > 0)
        {
            slipsQuery = slipsQuery.Where(slip => slip.Worker!.TradeId.HasValue && selectedTradeIds.Contains(slip.Worker.TradeId.Value));
        }

        if (DateFrom.HasValue)
        {
            var startDate = DateFrom.Value.Date;
            slipsQuery = slipsQuery.Where(slip => slip.DateTo >= startDate);
        }

        if (DateTo.HasValue)
        {
            var endDate = DateTo.Value.Date;
            slipsQuery = slipsQuery.Where(slip => slip.DateFrom <= endDate);
        }

        var slips = slipsQuery
            .OrderBy(slip => slip.Worker!.FirstName)
            .ThenBy(slip => slip.Worker!.LastName)
            .ThenByDescending(slip => slip.CreatedAt)
            .ToList();

        var workerBalanceRows = slips
            .GroupBy(slip => new
            {
                slip.WorkerId,
                WorkerName = $"{slip.Worker?.FirstName} {slip.Worker?.LastName}".Trim(),
                TradeName = slip.Worker?.Trade?.Name ?? "Unassigned"
            })
            .Select(group =>
            {
                var activeSlips = group
                    .Where(slip => slip.Status != PayrollSlipStatus.Cancelled)
                    .ToList();

                var totalEarned = Math.Round(activeSlips.Sum(slip => slip.TotalAmount), 2);
                var totalPaid = Math.Round(activeSlips.Sum(slip => slip.PayrollPayments.Sum(payment => payment.Amount)), 2);
                var remainingBalance = Math.Round(totalEarned - totalPaid, 2);
                var lastPaymentDate = activeSlips
                    .SelectMany(slip => slip.PayrollPayments)
                    .OrderByDescending(payment => payment.PaymentDate)
                    .Select(payment => (DateTime?)payment.PaymentDate)
                    .FirstOrDefault();

                return new WorkerBalanceRow
                {
                    WorkerId = group.Key.WorkerId,
                    WorkerName = group.Key.WorkerName,
                    TradeName = group.Key.TradeName,
                    TotalEarned = totalEarned,
                    TotalPaid = totalPaid,
                    RemainingBalance = remainingBalance,
                    PayrollSlipsCount = activeSlips.Count,
                    LastPaymentDate = lastPaymentDate,
                    PaymentStatus = GetPaymentStatus(totalEarned, totalPaid, remainingBalance)
                };
            })
            .Where(row => MatchesBalanceStatus(row, SelectedBalanceStatusOption?.Value))
            .OrderBy(row => row.WorkerName)
            .ToList();

        var selectedWorkerId = SelectedWorkerBalance?.WorkerId;

        WorkerBalances.Clear();

        foreach (var row in workerBalanceRows)
        {
            WorkerBalances.Add(row);
        }

        TotalEarnedSummary = Math.Round(workerBalanceRows.Sum(row => row.TotalEarned), 2);
        TotalPaidSummary = Math.Round(workerBalanceRows.Sum(row => row.TotalPaid), 2);
        TotalRemainingBalanceSummary = Math.Round(workerBalanceRows.Sum(row => row.RemainingBalance), 2);
        WorkersWithBalanceSummary = workerBalanceRows.Count(row => row.RemainingBalance > 0);

        SelectedWorkerBalance = WorkerBalances.FirstOrDefault(row => row.WorkerId == selectedWorkerId)
                                ?? WorkerBalances.FirstOrDefault();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        isInitializing = true;
        DateFrom = null;
        DateTo = null;
        SelectedBalanceStatusOption = BalanceStatusOptions.FirstOrDefault();

        foreach (var option in WorkerOptions)
        {
            option.IsSelected = false;
        }

        foreach (var option in TradeOptions)
        {
            option.IsSelected = false;
        }

        isInitializing = false;
        LoadBalances();
    }

    private void LoadLookupData()
    {
        using var context = new AppDbContext();

        var workers = context.Workers
            .AsNoTracking()
            .Include(worker => worker.Trade)
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new SelectableLookupOption
            {
                Id = worker.Id,
                Name = $"{worker.FirstName} {worker.LastName}"
            })
            .ToList();

        var trades = context.Trades
            .AsNoTracking()
            .OrderBy(trade => trade.Name)
            .Select(trade => new SelectableLookupOption
            {
                Id = trade.Id,
                Name = trade.Name
            })
            .ToList();

        WorkerOptions.Clear();
        TradeOptions.Clear();
        BalanceStatusOptions.Clear();

        foreach (var worker in workers)
        {
            worker.PropertyChanged += OnFilterOptionPropertyChanged;
            WorkerOptions.Add(worker);
        }

        foreach (var trade in trades)
        {
            trade.PropertyChanged += OnFilterOptionPropertyChanged;
            TradeOptions.Add(trade);
        }

        BalanceStatusOptions.Add(new BalanceStatusOption { Name = "All", Value = WorkerBalanceStatus.All });
        BalanceStatusOptions.Add(new BalanceStatusOption { Name = "Fully Paid", Value = WorkerBalanceStatus.FullyPaid });
        BalanceStatusOptions.Add(new BalanceStatusOption { Name = "Partially Paid", Value = WorkerBalanceStatus.PartiallyPaid });
        BalanceStatusOptions.Add(new BalanceStatusOption { Name = "Unpaid / Has Balance", Value = WorkerBalanceStatus.HasBalance });

        SelectedBalanceStatusOption ??= BalanceStatusOptions.FirstOrDefault();
    }

    private void LoadSelectedWorkerDetails()
    {
        SelectedWorkerPayrollSlips.Clear();
        SelectedWorkerPayments.Clear();

        if (SelectedWorkerBalance is null)
        {
            SelectedWorkerName = "Select a worker balance to view details.";
            return;
        }

        using var context = new AppDbContext();

        var slipsQuery = context.PayrollSlips
            .AsNoTracking()
            .Include(slip => slip.PayrollPayments)
            .Where(slip => slip.WorkerId == SelectedWorkerBalance.WorkerId);

        if (DateFrom.HasValue)
        {
            var startDate = DateFrom.Value.Date;
            slipsQuery = slipsQuery.Where(slip => slip.DateTo >= startDate);
        }

        if (DateTo.HasValue)
        {
            var endDate = DateTo.Value.Date;
            slipsQuery = slipsQuery.Where(slip => slip.DateFrom <= endDate);
        }

        var slips = slipsQuery
            .OrderByDescending(slip => slip.CreatedAt)
            .ThenByDescending(slip => slip.Id)
            .ToList();

        foreach (var slip in slips)
        {
            SelectedWorkerPayrollSlips.Add(new WorkerSlipHistoryRow
            {
                SlipNumber = slip.SlipNumber,
                DateFrom = slip.DateFrom,
                DateTo = slip.DateTo,
                TotalAmount = slip.TotalAmount,
                AmountPaid = slip.AmountPaid,
                RemainingBalance = slip.RemainingBalance,
                Status = slip.Status,
                CreatedAt = slip.CreatedAt
            });
        }

        foreach (var payment in slips
                     .Where(slip => slip.Status != PayrollSlipStatus.Cancelled)
                     .SelectMany(slip => slip.PayrollPayments.Select(item => new WorkerPaymentRow
                     {
                         PaymentDate = item.PaymentDate,
                         Amount = item.Amount,
                         Notes = item.Notes
                     }))
                     .OrderByDescending(payment => payment.PaymentDate))
        {
            SelectedWorkerPayments.Add(payment);
        }

        SelectedWorkerName = $"{SelectedWorkerBalance.WorkerName} ({SelectedWorkerBalance.TradeName})";
    }

    private void OnFilterOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableLookupOption.IsSelected) && !isInitializing)
        {
            LoadBalances();
        }
    }

    private static string GetPaymentStatus(decimal totalEarned, decimal totalPaid, decimal remainingBalance)
    {
        if (totalEarned > 0m && remainingBalance == 0m)
        {
            return "Fully Paid";
        }

        if (totalPaid > 0m && remainingBalance > 0m)
        {
            return "Partially Paid";
        }

        if (totalEarned > 0m && totalPaid == 0m)
        {
            return "Unpaid / Has Balance";
        }

        return "No Payroll";
    }

    private static bool MatchesBalanceStatus(WorkerBalanceRow row, WorkerBalanceStatus? status)
    {
        return status switch
        {
            null => true,
            WorkerBalanceStatus.All => true,
            WorkerBalanceStatus.FullyPaid => row.PaymentStatus == "Fully Paid",
            WorkerBalanceStatus.PartiallyPaid => row.PaymentStatus == "Partially Paid",
            WorkerBalanceStatus.HasBalance => row.PaymentStatus == "Unpaid / Has Balance" || row.PaymentStatus == "Partially Paid",
            _ => true
        };
    }

    public class WorkerBalanceRow
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public decimal TotalEarned { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public int PayrollSlipsCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class WorkerSlipHistoryRow
    {
        public string SlipNumber { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public PayrollSlipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkerPaymentRow
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class BalanceStatusOption
    {
        public string Name { get; set; } = string.Empty;
        public WorkerBalanceStatus Value { get; set; }
    }

    public enum WorkerBalanceStatus
    {
        All = 0,
        FullyPaid = 1,
        PartiallyPaid = 2,
        HasBalance = 3
    }
}
