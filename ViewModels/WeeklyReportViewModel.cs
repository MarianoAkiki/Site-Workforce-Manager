using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager.ViewModels;

public partial class WeeklyReportViewModel : ObservableObject
{
    private CancellationTokenSource? loadCts;
    private readonly DateTime latestFullWeekStart;

    public WeeklyReportViewModel()
    {
        latestFullWeekStart = GetLatestFullWeekStart(DateTime.Today);
        WeekStart = latestFullWeekStart;
        PickerDate = DateTime.Today;
    }

    public ObservableCollection<WeeklyReportRow> Rows { get; } = new();
    public ObservableCollection<Trade> Trades { get; } = new();

    [ObservableProperty] private DateTime weekStart;
    [ObservableProperty] private DateTime? pickerDate;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private Trade? selectedTrade;

    public DateTime WeekEnd => WeekStart.AddDays(6);
    public string WeekRangeText => $"{WeekStart:ddd, MMM dd yyyy} – {WeekEnd:ddd, MMM dd yyyy}";
    public bool CanGoNextWeek => WeekStart < latestFullWeekStart;
    public DateTime MaxPickerDate => latestFullWeekStart;

    public string DayName0 => WeekStart.ToString("ddd");
    public string DayName1 => WeekStart.AddDays(1).ToString("ddd");
    public string DayName2 => WeekStart.AddDays(2).ToString("ddd");
    public string DayName3 => WeekStart.AddDays(3).ToString("ddd");
    public string DayName4 => WeekStart.AddDays(4).ToString("ddd");
    public string DayName5 => WeekStart.AddDays(5).ToString("ddd");
    public string DayName6 => WeekStart.AddDays(6).ToString("ddd");
    public string DayDate0 => WeekStart.ToString("MMM d");
    public string DayDate1 => WeekStart.AddDays(1).ToString("MMM d");
    public string DayDate2 => WeekStart.AddDays(2).ToString("MMM d");
    public string DayDate3 => WeekStart.AddDays(3).ToString("MMM d");
    public string DayDate4 => WeekStart.AddDays(4).ToString("MMM d");
    public string DayDate5 => WeekStart.AddDays(5).ToString("MMM d");
    public string DayDate6 => WeekStart.AddDays(6).ToString("MMM d");

    partial void OnWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekEnd));
        OnPropertyChanged(nameof(WeekRangeText));
        OnPropertyChanged(nameof(CanGoNextWeek));
        OnPropertyChanged(nameof(DayName0));
        OnPropertyChanged(nameof(DayName1));
        OnPropertyChanged(nameof(DayName2));
        OnPropertyChanged(nameof(DayName3));
        OnPropertyChanged(nameof(DayName4));
        OnPropertyChanged(nameof(DayName5));
        OnPropertyChanged(nameof(DayName6));
        OnPropertyChanged(nameof(DayDate0));
        OnPropertyChanged(nameof(DayDate1));
        OnPropertyChanged(nameof(DayDate2));
        OnPropertyChanged(nameof(DayDate3));
        OnPropertyChanged(nameof(DayDate4));
        OnPropertyChanged(nameof(DayDate5));
        OnPropertyChanged(nameof(DayDate6));
        _ = LoadRowsAsync();
    }

    partial void OnPickerDateChanged(DateTime? value)
    {
        if (value is null) return;
        var thursday = SnapToWeekStart(value.Value);
        if (thursday > latestFullWeekStart) thursday = latestFullWeekStart;
        if (thursday != WeekStart) WeekStart = thursday;
    }

    partial void OnSelectedTradeChanged(Trade? value) => _ = LoadRowsAsync();

    [RelayCommand]
    private void Print()
    {
        if (Rows.Count == 0) return;
        WeeklyReportPrintService.Print(WeekStart, WeekEnd, Rows.ToList());
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
        if (!CanGoNextWeek) return;
        WeekStart = WeekStart.AddDays(7);
        PickerDate = WeekStart;
    }

    public void LoadPage()
    {
        using var context = new AppDbContext();
        var trades = context.Trades.AsNoTracking().OrderBy(t => t.Name).ToList();
        Trades.Clear();
        foreach (var t in trades) Trades.Add(t);
        SelectedTrade = Trades.FirstOrDefault();
        _ = LoadRowsAsync();
    }

    private async Task LoadRowsAsync()
    {
        if (SelectedTrade is null) return;

        loadCts?.Cancel();
        loadCts = new CancellationTokenSource();
        var cts = loadCts;

        IsLoading = true;
        Rows.Clear();

        var weekStart = WeekStart;
        var weekEnd = WeekEnd;
        var selectedTradeId = SelectedTrade.Id;

        try
        {
            var data = await Task.Run(() =>
            {
                using var context = new AppDbContext();

                var workersQuery = context.Workers
                    .AsNoTracking()
                    .Include(w => w.Trade)
                    .Where(w => w.Status == EntityStatus.Active);

                workersQuery = workersQuery.Where(w => w.TradeId == selectedTradeId);

                var workers = workersQuery
                    .OrderBy(w => w.Trade!.Name)
                    .ThenBy(w => w.FirstName)
                    .ThenBy(w => w.LastName)
                    .ToList();

                var workerIds = workers.Select(w => w.Id).ToList();

                var allLogs = context.WorkLogs
                    .AsNoTracking()
                    .Where(log => workerIds.Contains(log.WorkerId) && log.WorkDate <= weekEnd.Date)
                    .ToList();

                var allPayments = context.WorkerPayments
                    .AsNoTracking()
                    .Where(p => workerIds.Contains(p.WorkerId) && p.PaymentDate <= weekEnd.Date)
                    .ToList();

                var allRates = context.WorkerRateHistories
                    .AsNoTracking()
                    .Where(r => workerIds.Contains(r.WorkerId))
                    .ToList();

                return (workers, allLogs, allPayments, allRates);
            }, cts.Token);

            if (cts.IsCancellationRequested) return;

            foreach (var worker in data.workers)
            {
                var workerLogs = data.allLogs.Where(l => l.WorkerId == worker.Id).ToList();
                var workerPayments = data.allPayments.Where(p => p.WorkerId == worker.Id).ToList();
                var workerRates = data.allRates.Where(r => r.WorkerId == worker.Id).ToList();

                var weekLogs = workerLogs.Where(l => l.WorkDate >= weekStart.Date && l.WorkDate <= weekEnd.Date).ToList();

                var dayHours = new decimal[7];
                foreach (var log in weekLogs)
                {
                    var idx = (int)(log.WorkDate.Date - weekStart.Date).TotalDays;
                    if (idx >= 0 && idx < 7)
                        dayHours[idx] = log.DurationHours;
                }

                var totalHours = dayHours.Sum();
                var numberOfDays = dayHours.Count(h => h > 0);

                var earnedBeforeWeek = (decimal)workerLogs
                    .Where(l => l.WorkDate < weekStart.Date)
                    .Sum(l => (double)l.TotalAmount);
                var weekEarnings = (decimal)weekLogs.Sum(l => (double)l.TotalAmount);
                var totalEarnedUpToWeekEnd = earnedBeforeWeek + weekEarnings;

                var paidBeforeWeek = (decimal)workerPayments
                    .Where(p => p.PaymentDate < weekStart.Date)
                    .Sum(p => (double)p.Amount);
                var totalPaidUpToWeekEnd = (decimal)workerPayments.Sum(p => (double)p.Amount);

                var balanceBeforeWeek = Math.Round(earnedBeforeWeek - paidBeforeWeek, 2);
                var totalBalanceTillWeekEnd = Math.Round(totalEarnedUpToWeekEnd - totalPaidUpToWeekEnd, 2);

                var dailyRate = workerRates
                    .Where(r => r.EffectiveFrom <= weekStart.Date)
                    .OrderByDescending(r => r.EffectiveFrom)
                    .FirstOrDefault()?.DailyRate ?? 0m;

                Rows.Add(new WeeklyReportRow
                {
                    WorkerId = worker.Id,
                    WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                    Trade = worker.Trade?.Name ?? string.Empty,
                    DailyRate = dailyRate,
                    Day0 = dayHours[0],
                    Day1 = dayHours[1],
                    Day2 = dayHours[2],
                    Day3 = dayHours[3],
                    Day4 = dayHours[4],
                    Day5 = dayHours[5],
                    Day6 = dayHours[6],
                    TotalHours = Math.Round(totalHours, 2),
                    NumberOfDays = numberOfDays,
                    BalanceBeforeWeek = balanceBeforeWeek,
                    WeekEarnings = Math.Round(weekEarnings, 2),
                    TotalEarnedUpToWeekEnd = Math.Round(totalEarnedUpToWeekEnd, 2),
                    TotalPaidUpToWeekEnd = Math.Round(totalPaidUpToWeekEnd, 2),
                    TotalBalanceTillWeekEnd = totalBalanceTillWeekEnd,
                });
            }

            if (Rows.Count > 0)
            {
                Rows.Add(new WeeklyReportRow
                {
                    IsTotalsRow = true,
                    WorkerName = "TOTAL",
                    Day0 = Rows.Sum(r => r.Day0),
                    Day1 = Rows.Sum(r => r.Day1),
                    Day2 = Rows.Sum(r => r.Day2),
                    Day3 = Rows.Sum(r => r.Day3),
                    Day4 = Rows.Sum(r => r.Day4),
                    Day5 = Rows.Sum(r => r.Day5),
                    Day6 = Rows.Sum(r => r.Day6),
                    TotalHours = Math.Round(Rows.Sum(r => r.TotalHours), 2),
                    BalanceBeforeWeek = Math.Round(Rows.Sum(r => r.BalanceBeforeWeek), 2),
                    WeekEarnings = Math.Round(Rows.Sum(r => r.WeekEarnings), 2),
                    TotalEarnedUpToWeekEnd = Math.Round(Rows.Sum(r => r.TotalEarnedUpToWeekEnd), 2),
                    TotalPaidUpToWeekEnd = Math.Round(Rows.Sum(r => r.TotalPaidUpToWeekEnd), 2),
                    TotalBalanceTillWeekEnd = Math.Round(Rows.Sum(r => r.TotalBalanceTillWeekEnd), 2),
                });
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (!cts.IsCancellationRequested) IsLoading = false;
        }
    }

    private static DateTime GetLatestFullWeekStart(DateTime today)
    {
        var daysSinceWednesday = ((int)today.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;
        return today.Date.AddDays(-daysSinceWednesday).AddDays(-6);
    }

    private static DateTime SnapToWeekStart(DateTime date)
    {
        var daysSinceThursday = ((int)date.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return date.Date.AddDays(-daysSinceThursday);
    }
}

public class WeeklyReportRow
{
    public bool IsTotalsRow { get; set; }
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string Trade { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public decimal Day0 { get; set; }
    public decimal Day1 { get; set; }
    public decimal Day2 { get; set; }
    public decimal Day3 { get; set; }
    public decimal Day4 { get; set; }
    public decimal Day5 { get; set; }
    public decimal Day6 { get; set; }
    public decimal TotalHours { get; set; }
    public int NumberOfDays { get; set; }
    public decimal BalanceBeforeWeek { get; set; }
    public decimal WeekEarnings { get; set; }
    public decimal TotalEarnedUpToWeekEnd { get; set; }
    public decimal TotalPaidUpToWeekEnd { get; set; }
    public decimal TotalBalanceTillWeekEnd { get; set; }

    public string Day0Display => FormatHours(Day0);
    public string Day1Display => FormatHours(Day1);
    public string Day2Display => FormatHours(Day2);
    public string Day3Display => FormatHours(Day3);
    public string Day4Display => FormatHours(Day4);
    public string Day5Display => FormatHours(Day5);
    public string Day6Display => FormatHours(Day6);
    public string TotalHoursDisplay => TotalHours > 0 ? TotalHours.ToString("0.##") : string.Empty;
    public string DailyRateDisplay => DailyRate > 0 ? DailyRate.ToString("C") : string.Empty;
    public string BalanceBeforeWeekDisplay => FmtBalance(BalanceBeforeWeek);
    public string WeekEarningsDisplay => WeekEarnings > 0 ? WeekEarnings.ToString("C") : string.Empty;
    public string TotalEarnedDisplay => TotalEarnedUpToWeekEnd > 0 ? TotalEarnedUpToWeekEnd.ToString("C") : string.Empty;
    public string TotalPaidDisplay => TotalPaidUpToWeekEnd > 0 ? TotalPaidUpToWeekEnd.ToString("C") : string.Empty;
    public string TotalBalanceDisplay => FmtBalance(TotalBalanceTillWeekEnd);

    private static string FormatHours(decimal h) => h > 0 ? h.ToString("0.##") : string.Empty;
    private static string FmtBalance(decimal v) =>
        v == 0 ? string.Empty : v < 0 ? $"-{Math.Abs(v):C}" : v.ToString("C");
}
