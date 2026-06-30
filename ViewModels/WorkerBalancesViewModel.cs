using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class WorkerBalancesViewModel : ObservableObject
{
    private readonly List<WorkerBalanceRow> allRows = new();
    public PagedList<WorkerBalanceRow> RowsPage { get; } = new(25);

    [ObservableProperty]
    private string workerIdFilterText = string.Empty;

    [ObservableProperty]
    private string workerNameFilterText = string.Empty;

    [ObservableProperty]
    private string tradeFilterText = string.Empty;

    [ObservableProperty]
    private bool showOnlyWithBalance;

    [ObservableProperty]
    private decimal grandTotalEarned;

    [ObservableProperty]
    private decimal grandTotalPaid;

    [ObservableProperty]
    private decimal grandTotalBalance;

    public ObservableCollection<WorkerBalanceRow> FilteredRows { get; } = new();

    public string GrandTotalEarnedDisplay => GrandTotalEarned.ToString("C");
    public string GrandTotalPaidDisplay => GrandTotalPaid.ToString("C");
    public string GrandTotalBalanceDisplay => GrandTotalBalance < 0
        ? $"-{Math.Abs(GrandTotalBalance):C}"
        : GrandTotalBalance.ToString("C");

    partial void OnWorkerIdFilterTextChanged(string value) => RefreshFilteredRows();
    partial void OnWorkerNameFilterTextChanged(string value) => RefreshFilteredRows();
    partial void OnTradeFilterTextChanged(string value) => RefreshFilteredRows();
    partial void OnShowOnlyWithBalanceChanged(bool value) => RefreshFilteredRows();

    public void LoadPage()
    {
        WorkerIdFilterText = string.Empty;
        WorkerNameFilterText = string.Empty;
        TradeFilterText = string.Empty;
        ShowOnlyWithBalance = false;
        LoadRows();
    }

    [RelayCommand]
    private void ClearWorkerIdFilter() => WorkerIdFilterText = string.Empty;

    [RelayCommand]
    private void ClearWorkerNameFilter() => WorkerNameFilterText = string.Empty;

    [RelayCommand]
    private void ClearTradeFilter() => TradeFilterText = string.Empty;

    private void LoadRows()
    {
        allRows.Clear();
        FilteredRows.Clear();

        using var context = new AppDbContext();

        var workers = context.Workers
            .AsNoTracking()
            .Include(w => w.Trade)
            .Where(w => w.Status == EntityStatus.Active)
            .OrderBy(w => w.Trade!.Name)
            .ThenBy(w => w.FirstName)
            .ThenBy(w => w.LastName)
            .ToList();

        var workerIds = workers.Select(w => w.Id).ToList();

        var earnedByWorker = context.WorkLogs
            .Where(log => workerIds.Contains(log.WorkerId))
            .Select(log => new { log.WorkerId, log.TotalAmount })
            .AsEnumerable()
            .GroupBy(x => x.WorkerId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

        var paidByWorker = context.WorkerPayments
            .Where(p => workerIds.Contains(p.WorkerId))
            .Select(p => new { p.WorkerId, p.Amount })
            .AsEnumerable()
            .GroupBy(x => x.WorkerId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        foreach (var worker in workers)
        {
            earnedByWorker.TryGetValue(worker.Id, out var earned);
            paidByWorker.TryGetValue(worker.Id, out var paid);
            var balance = Math.Round(earned - paid, 2);

            allRows.Add(new WorkerBalanceRow
            {
                WorkerId = worker.Id,
                WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                TradeName = worker.Trade?.Name ?? string.Empty,
                TotalEarned = Math.Round(earned, 2),
                TotalPaid = Math.Round(paid, 2),
                Balance = balance
            });
        }

        RefreshFilteredRows();
    }

    private void RefreshFilteredRows()
    {
        FilteredRows.Clear();

        var idFilter = WorkerIdFilterText.Trim();
        var nameFilter = WorkerNameFilterText.Trim();
        var tradeFilter = TradeFilterText.Trim();

        foreach (var row in allRows)
        {
            if (!string.IsNullOrWhiteSpace(idFilter) &&
                !row.WorkerId.ToString().Contains(idFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(nameFilter) &&
                !row.WorkerName.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(tradeFilter) &&
                !row.TradeName.Contains(tradeFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (ShowOnlyWithBalance && row.Balance == 0m)
                continue;

            FilteredRows.Add(row);
        }

        GrandTotalEarned = Math.Round(FilteredRows.Sum(r => r.TotalEarned), 2);
        GrandTotalPaid = Math.Round(FilteredRows.Sum(r => r.TotalPaid), 2);
        GrandTotalBalance = Math.Round(FilteredRows.Sum(r => r.Balance), 2);
        OnPropertyChanged(nameof(GrandTotalEarnedDisplay));
        OnPropertyChanged(nameof(GrandTotalPaidDisplay));
        OnPropertyChanged(nameof(GrandTotalBalanceDisplay));

        RowsPage.SetSource(FilteredRows);
    }
}

public class WorkerBalanceRow
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    public string TotalEarnedDisplay => TotalEarned.ToString("C");
    public string TotalPaidDisplay => TotalPaid.ToString("C");
    public string BalanceDisplay => Balance < 0 ? $"-{Math.Abs(Balance):C}" : Balance.ToString("C");
}
