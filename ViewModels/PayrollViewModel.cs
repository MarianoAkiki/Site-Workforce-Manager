using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public PayrollViewModel()
    {
        latestFullWeekStart = GetLatestFullWeekStart(DateTime.Today);
        WeekStart = latestFullWeekStart;
        LoadPayrollPage();
    }

    public ObservableCollection<PayrollTradeGroup> TradeGroups { get; } = new();
    public ObservableCollection<PayrollTradeGroup> FilteredTradeGroups { get; } = new();

    [ObservableProperty]
    private DateTime weekStart;

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

    public DateTime WeekEnd => WeekStart.AddDays(6);
    public string WeekRangeText => $"{WeekStart:dddd, MMM dd, yyyy} - {WeekEnd:dddd, MMM dd, yyyy}";
    public bool CanGoNextWeek => WeekStart < latestFullWeekStart;
    public bool CanEditSelectedWeek => WeekStart == latestFullWeekStart;
    public string BalanceHeaderText => CanEditSelectedWeek ? "Balance" : string.Empty;
    public string PaymentHeaderText => CanEditSelectedWeek ? "Payment Amount" : "Paid This Week";
    public string GrandTotalBalanceDisplay => CanEditSelectedWeek ? GrandTotalBalance.ToString("C") : string.Empty;
    public string GrandTotalPaymentDisplay => GrandTotalPayment.ToString("C");

    partial void OnWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekEnd));
        OnPropertyChanged(nameof(WeekRangeText));
        OnPropertyChanged(nameof(CanGoNextWeek));
        OnPropertyChanged(nameof(CanEditSelectedWeek));
        OnPropertyChanged(nameof(BalanceHeaderText));
        OnPropertyChanged(nameof(PaymentHeaderText));
        LoadPayrollRows();
    }

    partial void OnWorkerIdFilterTextChanged(string value) => RefreshFilteredGroups();
    partial void OnWorkerNameFilterTextChanged(string value) => RefreshFilteredGroups();
    partial void OnTradeFilterTextChanged(string value) => RefreshFilteredGroups();

    public void LoadPayrollPage()
    {
        WorkerIdFilterText = string.Empty;
        WorkerNameFilterText = string.Empty;
        TradeFilterText = string.Empty;
        LoadPayrollRows();
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        WeekStart = WeekStart.AddDays(-7);
    }

    [RelayCommand]
    private void NextWeek()
    {
        if (!CanGoNextWeek)
        {
            return;
        }

        WeekStart = WeekStart.AddDays(7);
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
    private void SavePayments()
    {
        if (!CanEditSelectedWeek)
        {
            MessageBox.Show("Previous payroll weeks are read-only. Only the latest completed week can be edited.");
            return;
        }

        using var context = new AppDbContext();
        using var transaction = context.Database.BeginTransaction();
        var savedCount = 0;

        foreach (var row in allTradeGroups.SelectMany(group => group.Rows))
        {
            if (string.IsNullOrWhiteSpace(row.PaymentAmountText))
            {
                RemoveExistingWeeklyPayment(context, row);
                continue;
            }

            if (!decimal.TryParse(row.PaymentAmountText, out var paymentAmount))
            {
                MessageBox.Show($"Please enter a valid payment amount for {row.WorkerName}.");
                return;
            }

            if (paymentAmount < 0)
            {
                MessageBox.Show($"Payment amount cannot be negative for {row.WorkerName}.");
                return;
            }

            if (paymentAmount == 0)
            {
                RemoveExistingWeeklyPayment(context, row);
                continue;
            }

            if (paymentAmount > row.Balance)
            {
                MessageBox.Show($"Payment amount cannot exceed the balance for {row.WorkerName}.");
                return;
            }

            var existingPayment = FindExistingWeeklyPayment(context, row.WorkerId);
            var now = DateTime.Now;

            if (existingPayment is null)
            {
                context.WorkerPayments.Add(new WorkerPayment
                {
                    WorkerId = row.WorkerId,
                    PaymentDate = WeekEnd.Date,
                    Amount = Math.Round(paymentAmount, 2),
                    Notes = BuildWeeklyPaymentNote(),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                existingPayment.Amount = Math.Round(paymentAmount, 2);
                existingPayment.Notes = BuildWeeklyPaymentNote();
                existingPayment.UpdatedAt = now;
            }

            savedCount++;
        }

        context.SaveChanges();
        transaction.Commit();

        LoadPayrollRows();
        StatusMessage = savedCount == 0
            ? "No payroll payments were saved."
            : $"{savedCount} payroll payments saved.";

        if (savedCount > 0)
        {
            ToastNotificationService.ShowSuccess(StatusMessage);
        }
    }

    private void RemoveExistingWeeklyPayment(AppDbContext context, PayrollWorkerRow row)
    {
        var existingPayment = FindExistingWeeklyPayment(context, row.WorkerId);

        if (existingPayment is not null)
        {
            context.WorkerPayments.Remove(existingPayment);
        }
    }

    private WorkerPayment? FindExistingWeeklyPayment(AppDbContext context, int workerId)
    {
        var paymentNote = BuildWeeklyPaymentNote();

        return context.WorkerPayments
            .FirstOrDefault(payment =>
                payment.WorkerId == workerId &&
                payment.PaymentDate == WeekEnd.Date &&
                payment.Notes == paymentNote);
    }

    private string BuildWeeklyPaymentNote()
    {
        return $"Weekly payroll payment for {WeekStart:yyyy-MM-dd} to {WeekEnd:yyyy-MM-dd}";
    }

    private void LoadPayrollRows()
    {
        allTradeGroups.Clear();
        TradeGroups.Clear();
        FilteredTradeGroups.Clear();

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
        var earnedLogs = context.WorkLogs
            .AsNoTracking()
            .Where(log => workerIds.Contains(log.WorkerId) && log.WorkDate <= WeekEnd.Date)
            .Select(log => new
            {
                log.WorkerId,
                log.TotalAmount
            })
            .ToList();

        var earnedByWorker = earnedLogs
            .GroupBy(log => log.WorkerId)
            .ToDictionary(group => group.Key, group => group.Sum(log => log.TotalAmount));

        var paymentsBeforeSelectedWeek = context.WorkerPayments
            .AsNoTracking()
            .Where(payment => workerIds.Contains(payment.WorkerId) && payment.PaymentDate < WeekStart.Date)
            .Select(payment => new
            {
                payment.WorkerId,
                payment.Amount
            })
            .ToList();

        var paidBeforeSelectedWeekByWorker = paymentsBeforeSelectedWeek
            .GroupBy(payment => payment.WorkerId)
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

        var paymentsDuringSelectedWeek = context.WorkerPayments
            .AsNoTracking()
            .Where(payment =>
                workerIds.Contains(payment.WorkerId) &&
                payment.PaymentDate >= WeekStart.Date &&
                payment.PaymentDate <= WeekEnd.Date)
            .Select(payment => new
            {
                payment.WorkerId,
                payment.Amount
            })
            .ToList();

        var paidDuringSelectedWeekByWorker = paymentsDuringSelectedWeek
            .GroupBy(payment => payment.WorkerId)
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

        var weeklyPaymentNote = BuildWeeklyPaymentNote();
        var weeklyPayrollPayments = context.WorkerPayments
            .AsNoTracking()
            .Where(payment =>
                workerIds.Contains(payment.WorkerId) &&
                payment.PaymentDate == WeekEnd.Date &&
                payment.Notes == weeklyPaymentNote)
            .ToDictionary(payment => payment.WorkerId, payment => payment.Amount);

        foreach (var tradeGroup in workers.GroupBy(worker => worker.Trade?.Name ?? "No Trade").OrderBy(group => group.Key))
        {
            var payrollGroup = new PayrollTradeGroup
            {
                TradeName = tradeGroup.Key
            };

            foreach (var worker in tradeGroup)
            {
                earnedByWorker.TryGetValue(worker.Id, out var earnedAmount);
                paidBeforeSelectedWeekByWorker.TryGetValue(worker.Id, out var paidBeforeSelectedWeek);
                paidDuringSelectedWeekByWorker.TryGetValue(worker.Id, out var paidDuringSelectedWeek);
                weeklyPayrollPayments.TryGetValue(worker.Id, out var editableWeeklyPayment);

                var row = new PayrollWorkerRow
                {
                    WorkerId = worker.Id,
                    WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                    TradeName = tradeGroup.Key,
                    Balance = CanEditSelectedWeek ? Math.Round(earnedAmount - paidBeforeSelectedWeek, 2) : 0m,
                    PaymentAmountText = CanEditSelectedWeek
                        ? (editableWeeklyPayment > 0 ? editableWeeklyPayment.ToString("0.##") : string.Empty)
                        : paidDuringSelectedWeek.ToString("0.##"),
                    IsPaymentEditable = CanEditSelectedWeek
                };

                row.PropertyChanged += OnPayrollRowPropertyChanged;
                payrollGroup.Rows.Add(row);
            }

            payrollGroup.RecalculateTotals(CanEditSelectedWeek);
            allTradeGroups.Add(payrollGroup);
            TradeGroups.Add(payrollGroup);
        }

        RefreshFilteredGroups();
        RecalculateGrandTotals();

        StatusMessage = CanEditSelectedWeek
            ? "Latest completed payroll week is editable."
            : "Previous payroll weeks are read-only.";
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

            filteredGroup.RecalculateTotals(CanEditSelectedWeek);
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
            group.RecalculateTotals(CanEditSelectedWeek);
        }

        foreach (var group in FilteredTradeGroups)
        {
            group.RecalculateTotals(CanEditSelectedWeek);
        }

        GrandTotalBalance = CanEditSelectedWeek
            ? Math.Round(FilteredTradeGroups.Sum(group => group.TotalBalance), 2)
            : 0m;
        GrandTotalPayment = Math.Round(FilteredTradeGroups.Sum(group => group.TotalPayment), 2);
        OnPropertyChanged(nameof(GrandTotalBalanceDisplay));
        OnPropertyChanged(nameof(GrandTotalPaymentDisplay));
    }

    private static DateTime GetLatestFullWeekStart(DateTime today)
    {
        var daysSinceWednesday = ((int)today.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;

        if (daysSinceWednesday == 0)
        {
            daysSinceWednesday = 7;
        }

        var lastCompletedWednesday = today.Date.AddDays(-daysSinceWednesday);
        return lastCompletedWednesday.AddDays(-6);
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

    public void RecalculateTotals(bool includeBalance)
    {
        ShowBalance = includeBalance;
        TotalBalance = includeBalance ? Math.Round(Rows.Sum(row => row.Balance), 2) : 0m;
        TotalPayment = Math.Round(Rows.Sum(row => row.PaymentAmount), 2);
        OnPropertyChanged(nameof(TotalBalanceDisplay));
        OnPropertyChanged(nameof(TotalPaymentDisplay));
    }

    public bool ShowBalance { get; private set; }
    public string TotalBalanceDisplay => ShowBalance ? TotalBalance.ToString("C") : string.Empty;
    public string TotalPaymentDisplay => TotalPayment.ToString("C");
}

public partial class PayrollWorkerRow : ObservableObject
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string BalanceDisplay => Balance > 0 ? Balance.ToString("C") : string.Empty;

    [ObservableProperty]
    private string paymentAmountText = string.Empty;

    [ObservableProperty]
    private bool isPaymentEditable;

    public bool IsPaymentReadOnly => !IsPaymentEditable;

    partial void OnIsPaymentEditableChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPaymentReadOnly));
    }

    public decimal PaymentAmount => decimal.TryParse(PaymentAmountText, out var amount) ? amount : 0m;
}
