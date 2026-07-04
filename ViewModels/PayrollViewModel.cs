using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager.ViewModels;

public partial class PayrollViewModel : ObservableObject
{
    private readonly DateTime latestFullWeekStart;
    private readonly List<PayrollTradeGroup> allTradeGroups = new();
    private CancellationTokenSource? loadCts;

    public PayrollViewModel()
    {
        latestFullWeekStart = GetLatestFullWeekStart(DateTime.Today);
        WeekStart = latestFullWeekStart;
        PickerDate = DateTime.Today;
        LoadPayrollPage();
    }

    public ObservableCollection<PayrollTradeGroup> TradeGroups { get; } = new();
    public ObservableCollection<PayrollTradeGroup> FilteredTradeGroups { get; } = new();

    [ObservableProperty]
    private DateTime weekStart;

    [ObservableProperty]
    private DateTime? pickerDate;

    [ObservableProperty]
    private string workerIdFilterText = string.Empty;

    [ObservableProperty]
    private string workerNameFilterText = string.Empty;

    [ObservableProperty]
    private string tradeFilterText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private decimal grandTotalBalance;

    [ObservableProperty]
    private decimal grandTotalPayment;

    [ObservableProperty]
    private bool isLoading;

    public DateTime WeekEnd => WeekStart.AddDays(6);
    public string WeekRangeText => $"{WeekStart:dddd, MMM dd, yyyy} - {WeekEnd:dddd, MMM dd, yyyy}";
    public bool CanGoNextWeek => WeekStart < latestFullWeekStart;
    public bool CanEditSelectedWeek => WeekStart == latestFullWeekStart;
    public DateTime MaxPickerDate => latestFullWeekStart;
    public string BalanceHeaderText => "Balance";
    public string PaymentHeaderText => CanEditSelectedWeek ? "Payment Amount" : "Paid This Week";
    public string GrandTotalBalanceDisplay => PayrollFmt.Fmt(GrandTotalBalance);
    public string GrandTotalPaymentDisplay => PayrollFmt.Fmt(GrandTotalPayment);

    partial void OnPickerDateChanged(DateTime? value)
    {
        if (value is null) return;
        var thursday = SnapToWeekStart(value.Value);
        if (thursday > latestFullWeekStart) thursday = latestFullWeekStart;
        if (thursday != WeekStart)
            WeekStart = thursday;
    }

    partial void OnWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekEnd));
        OnPropertyChanged(nameof(WeekRangeText));
        OnPropertyChanged(nameof(CanGoNextWeek));
        OnPropertyChanged(nameof(CanEditSelectedWeek));
        OnPropertyChanged(nameof(BalanceHeaderText));
        OnPropertyChanged(nameof(PaymentHeaderText));
        _ = LoadPayrollRowsAsync();
    }

    partial void OnWorkerIdFilterTextChanged(string value) => RefreshFilteredGroups();
    partial void OnWorkerNameFilterTextChanged(string value) => RefreshFilteredGroups();
    partial void OnTradeFilterTextChanged(string value) => RefreshFilteredGroups();

    public void LoadPayrollPage()
    {
        WeekStart = latestFullWeekStart;
        PickerDate = DateTime.Today;
        WorkerIdFilterText = string.Empty;
        WorkerNameFilterText = string.Empty;
        TradeFilterText = string.Empty;
        _ = LoadPayrollRowsAsync();
    }

    [RelayCommand]
    private void GoToToday()
    {
        WeekStart = latestFullWeekStart;
        PickerDate = DateTime.Today;
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        WeekStart = WeekStart.AddDays(-7);
        PickerDate = WeekStart;
    }

    [RelayCommand]
    private void NextWeek()
    {
        if (!CanGoNextWeek)
        {
            return;
        }

        WeekStart = WeekStart.AddDays(7);
        PickerDate = WeekStart;
    }

    [RelayCommand]
    private void ClearWorkerIdFilter()
    {
        WorkerIdFilterText = string.Empty;
    }

    [RelayCommand]
    private void ClearWorkerNameFilter()
    {
        WorkerNameFilterText = string.Empty;
    }

    [RelayCommand]
    private void ClearTradeFilter()
    {
        TradeFilterText = string.Empty;
    }

    [RelayCommand]
    private void Print()
    {
        PayrollPrintService.Print(
            WeekStart, WeekEnd,
            FilteredTradeGroups.ToList(),
            GrandTotalBalance, GrandTotalPayment);
    }

    private void AutoSaveRow(PayrollWorkerRow row)
    {
        if (!CanEditSelectedWeek)
            return;

        using var context = new AppDbContext();

        if (string.IsNullOrWhiteSpace(row.PaymentAmountText))
        {
            RemoveExistingWeeklyPayment(context, row);
            context.SaveChanges();
            StatusMessage = $"Payment cleared for {row.WorkerName}.";
            RecalculateGrandTotals();
            return;
        }

        if (!decimal.TryParse(row.PaymentAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var paymentAmount))
        {
            StatusMessage = $"Invalid payment amount for {row.WorkerName}.";
            return;
        }

        if (paymentAmount < 0)
        {
            StatusMessage = $"Payment amount cannot be negative for {row.WorkerName}.";
            return;
        }

        if (paymentAmount == 0)
        {
            RemoveExistingWeeklyPayment(context, row);
            context.SaveChanges();
            StatusMessage = $"Payment cleared for {row.WorkerName}.";
            RecalculateGrandTotals();
            return;
        }

        var existingPayment = FindExistingWeeklyPayment(context, row.WorkerId);
        var now = DateTime.Now;

        if (existingPayment is null)
        {
            context.WorkerPayments.Add(new WorkerPayment
            {
                WorkerId = row.WorkerId,
                PaymentDate = DateTime.Today,
                Amount = Math.Round(paymentAmount, 2),
                WeekStartDate = WeekStart.Date,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existingPayment.Amount = Math.Round(paymentAmount, 2);
            existingPayment.PaymentDate = DateTime.Today;
            existingPayment.UpdatedAt = now;
        }

        context.SaveChanges();
        StatusMessage = $"Saved payment for {row.WorkerName}.";
        RecalculateGrandTotals();
    }

    private void RemoveExistingWeeklyPayment(AppDbContext context, PayrollWorkerRow row)
    {
        var existingPayment = FindExistingWeeklyPayment(context, row.WorkerId);

        if (existingPayment is not null)
        {
            context.WorkerPayments.Remove(existingPayment);
        }
    }

    private void ComputeLiveBalance(PayrollWorkerRow row)
    {
        using var context = new AppDbContext();
        var totalEarned = context.WorkLogs
            .Where(log => log.WorkerId == row.WorkerId)
            .Select(log => log.TotalAmount)
            .AsEnumerable()
            .Sum();
        var totalPaid = context.WorkerPayments
            .Where(payment => payment.WorkerId == row.WorkerId)
            .Select(payment => payment.Amount)
            .AsEnumerable()
            .Sum();
        row.LiveBalance = Math.Round(totalEarned - totalPaid, 2);
    }

    private WorkerPayment? FindExistingWeeklyPayment(AppDbContext context, int workerId)
    {
        return context.WorkerPayments
            .FirstOrDefault(payment =>
                payment.WorkerId == workerId &&
                payment.WeekStartDate == WeekStart.Date);
    }

    private async Task LoadPayrollRowsAsync()
    {
        loadCts?.Cancel();
        loadCts = new CancellationTokenSource();
        var cts = loadCts;

        IsLoading = true;
        allTradeGroups.Clear();
        TradeGroups.Clear();
        FilteredTradeGroups.Clear();

        // Capture values used in background thread to avoid cross-thread access to the VM
        var weekStart = WeekStart;
        var weekEnd = WeekEnd;
        var canEdit = CanEditSelectedWeek;

        try
        {
            var data = await Task.Run(() =>
            {
                using var context = new AppDbContext();

                var workers = context.Workers
                    .AsNoTracking()
                    .Include(worker => worker.Trade)
                    .Where(worker => worker.Status == EntityStatus.Active)
                    .OrderBy(worker => worker.Trade!.Name)
                    .ThenBy(worker => worker.FirstName)
                    .ThenBy(worker => worker.LastName)
                    .ToList();

                var workerIds = workers.Select(worker => worker.Id).ToList();

                var earnedByWorker = context.WorkLogs
                    .AsNoTracking()
                    .Where(log => workerIds.Contains(log.WorkerId) && log.WorkDate <= weekEnd.Date)
                    .GroupBy(log => log.WorkerId)
                    .Select(g => new { WorkerId = g.Key, Total = g.Sum(log => (double)log.TotalAmount) })
                    .ToDictionary(x => x.WorkerId, x => (decimal)x.Total);

                var paidUpToWeekEndByWorker = context.WorkerPayments
                    .AsNoTracking()
                    .Where(payment => workerIds.Contains(payment.WorkerId) && payment.PaymentDate <= weekEnd.Date)
                    .GroupBy(payment => payment.WorkerId)
                    .Select(g => new { WorkerId = g.Key, Total = g.Sum(p => (double)p.Amount) })
                    .ToDictionary(x => x.WorkerId, x => (decimal)x.Total);

                var weeklyPayrollPayments = context.WorkerPayments
                    .AsNoTracking()
                    .Where(payment => workerIds.Contains(payment.WorkerId) && payment.WeekStartDate == weekStart.Date)
                    .ToDictionary(payment => payment.WorkerId, payment => payment.Amount);

                return (workers, earnedByWorker, paidUpToWeekEndByWorker, weeklyPayrollPayments);
            }, cts.Token);

            if (cts.IsCancellationRequested) return;

            foreach (var tradeGroup in data.workers.GroupBy(worker => worker.Trade?.Name ?? "No Category").OrderBy(group => group.Key))
            {
                var payrollGroup = new PayrollTradeGroup { TradeName = tradeGroup.Key };

                foreach (var worker in tradeGroup)
                {
                    data.earnedByWorker.TryGetValue(worker.Id, out var earnedAmount);
                    data.paidUpToWeekEndByWorker.TryGetValue(worker.Id, out var paidUpToWeekEnd);
                    data.weeklyPayrollPayments.TryGetValue(worker.Id, out var editableWeeklyPayment);

                    var row = new PayrollWorkerRow
                    {
                        WorkerId = worker.Id,
                        WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                        TradeName = tradeGroup.Key,
                        Balance = Math.Round(earnedAmount - paidUpToWeekEnd, 2),
                        PaymentAmountText = editableWeeklyPayment > 0 ? editableWeeklyPayment.ToString("0.##") : string.Empty,
                        IsPaymentEditable = canEdit,
                        AutoSaveRequested = canEdit ? AutoSaveRow : null,
                        LiveBalanceRequested = ComputeLiveBalance
                    };

                    row.PropertyChanged += OnPayrollRowPropertyChanged;
                    payrollGroup.Rows.Add(row);
                }

                payrollGroup.RecalculateTotals();
                allTradeGroups.Add(payrollGroup);
                TradeGroups.Add(payrollGroup);
            }

            RefreshFilteredGroups();
            RecalculateGrandTotals();

            StatusMessage = canEdit
                ? "Latest completed payroll week is editable."
                : "Previous payroll weeks are read-only.";
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (!cts.IsCancellationRequested)
                IsLoading = false;
        }
    }

    private void OnPayrollRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PayrollWorkerRow.PaymentAmountText))
        {
            RecalculateGrandTotals();
        }
    }

    private void RefreshFilteredGroups()
    {
        FilteredTradeGroups.Clear();

        foreach (var group in allTradeGroups)
        {
            var filteredRows = group.Rows
                .Where(row => MatchesFilter(row))
                .ToList();

            if (filteredRows.Count == 0)
            {
                continue;
            }

            var filteredGroup = new PayrollTradeGroup
            {
                TradeName = group.TradeName
            };

            foreach (var row in filteredRows)
            {
                filteredGroup.Rows.Add(row);
            }

            filteredGroup.RecalculateTotals();
            FilteredTradeGroups.Add(filteredGroup);
        }

        RecalculateGrandTotals();
    }

    private bool MatchesFilter(PayrollWorkerRow row)
    {
        var idFilter = WorkerIdFilterText.Trim();
        var nameFilter = WorkerNameFilterText.Trim();
        var tradeFilter = TradeFilterText.Trim();

        var matchesId = string.IsNullOrWhiteSpace(idFilter) ||
                        row.WorkerId.ToString().Contains(idFilter, StringComparison.CurrentCultureIgnoreCase);
        var matchesName = string.IsNullOrWhiteSpace(nameFilter) ||
                          row.WorkerName.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase);
        var matchesTrade = string.IsNullOrWhiteSpace(tradeFilter) ||
                           row.TradeName.Contains(tradeFilter, StringComparison.CurrentCultureIgnoreCase);

        return matchesId && matchesName && matchesTrade;
    }

    private void RecalculateGrandTotals()
    {
        foreach (var group in allTradeGroups)
        {
            group.RecalculateTotals();
        }

        foreach (var group in FilteredTradeGroups)
        {
            group.RecalculateTotals();
        }

        GrandTotalBalance = Math.Round(FilteredTradeGroups.Sum(group => group.TotalBalance), 2);
        GrandTotalPayment = Math.Round(FilteredTradeGroups.Sum(group => group.TotalPayment), 2);
        OnPropertyChanged(nameof(GrandTotalBalanceDisplay));
        OnPropertyChanged(nameof(GrandTotalPaymentDisplay));
    }

    private static DateTime GetLatestFullWeekStart(DateTime today)
    {
        // daysSinceWednesday = 0 when today IS Wednesday (week ends today) — no override needed.
        var daysSinceWednesday = ((int)today.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;
        var lastCompletedWednesday = today.Date.AddDays(-daysSinceWednesday);
        return lastCompletedWednesday.AddDays(-6);
    }

    private static DateTime SnapToWeekStart(DateTime date)
    {
        var daysSinceThursday = ((int)date.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return date.Date.AddDays(-daysSinceThursday);
    }
}

public partial class PayrollTradeGroup : ObservableObject
{
    public string TradeName { get; set; } = string.Empty;
    public ObservableCollection<PayrollWorkerRow> Rows { get; } = new();

    [ObservableProperty]
    private decimal totalBalance;

    [ObservableProperty]
    private decimal totalPayment;

    public void RecalculateTotals()
    {
        TotalBalance = Math.Round(Rows.Sum(row => row.Balance), 2);
        TotalPayment = Math.Round(Rows.Sum(row => row.PaymentAmount), 2);
        OnPropertyChanged(nameof(TotalBalanceDisplay));
        OnPropertyChanged(nameof(TotalPaymentDisplay));
    }

    public string TotalBalanceDisplay => PayrollFmt.Fmt(TotalBalance);
    public string TotalPaymentDisplay => TotalPayment == 0 ? string.Empty : PayrollFmt.Fmt(TotalPayment);
}

public partial class PayrollWorkerRow : ObservableObject
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string BalanceDisplay => PayrollFmt.Fmt(Balance);
    public Action<PayrollWorkerRow>? AutoSaveRequested { get; set; }
    public Action<PayrollWorkerRow>? LiveBalanceRequested { get; set; }

    [ObservableProperty]
    private string paymentAmountText = string.Empty;

    [ObservableProperty]
    private bool isPaymentEditable;

    [ObservableProperty]
    private bool isLiveBalancePopupOpen;

    [ObservableProperty]
    private decimal? liveBalance;

    public string LiveBalanceDisplay => LiveBalance.HasValue
        ? PayrollFmt.Fmt(LiveBalance.Value)
        : "...";

    partial void OnLiveBalanceChanged(decimal? value) => OnPropertyChanged(nameof(LiveBalanceDisplay));

    public bool IsPaymentReadOnly => !IsPaymentEditable;

    partial void OnIsPaymentEditableChanged(bool value) => OnPropertyChanged(nameof(IsPaymentReadOnly));

    partial void OnPaymentAmountTextChanged(string value) => AutoSaveRequested?.Invoke(this);

    [RelayCommand]
    private void ShowLiveBalance()
    {
        if (IsLiveBalancePopupOpen)
        {
            IsLiveBalancePopupOpen = false;
            return;
        }
        LiveBalance = null;
        IsLiveBalancePopupOpen = true;
        LiveBalanceRequested?.Invoke(this);
    }

    public decimal PaymentAmount => decimal.TryParse(PaymentAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;
}

internal static class PayrollFmt
{
    internal static string Fmt(decimal value) =>
        value < 0 ? $"({Math.Abs(value):C})" : value.ToString("C");
}
