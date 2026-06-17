using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager.ViewModels;

public partial class WeeklyWorkEntryViewModel : ObservableObject
{
    private readonly DateTime latestFullWeekStart;

    public WeeklyWorkEntryViewModel()
    {
        latestFullWeekStart = GetLatestFullWeekStart(DateTime.Today);
        WeekStart = latestFullWeekStart;
        LoadWeeklyEntryPage();
    }

    public ObservableCollection<LookupOption> TradeOptions { get; } = new();
    public ObservableCollection<WeekDayColumn> WeekDays { get; } = new();
    public ObservableCollection<WeeklyWorkerRow> WorkerRows { get; } = new();

    [ObservableProperty]
    private LookupOption? selectedTradeOption;

    [ObservableProperty]
    private DateTime weekStart;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public string WeekRangeText => $"{WeekStart:dddd, MMM dd, yyyy} - {WeekEnd:dddd, MMM dd, yyyy}";
    public DateTime WeekEnd => WeekStart.AddDays(6);
    public bool CanGoNextWeek => WeekStart < latestFullWeekStart;

    partial void OnSelectedTradeOptionChanged(LookupOption? value)
    {
        LoadWorkerRows();
    }

    partial void OnWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekEnd));
        OnPropertyChanged(nameof(WeekRangeText));
        OnPropertyChanged(nameof(CanGoNextWeek));
        LoadWeekDays();
        LoadWorkerRows();
    }

    public void LoadWeeklyEntryPage()
    {
        LoadTradeOptions();
        LoadWeekDays();
        LoadWorkerRows();
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
    private void SaveWeek()
    {
        if (SelectedTradeOption?.Id is not int)
        {
            MessageBox.Show("Please select a trade first.");
            return;
        }

        using var context = new AppDbContext();
        using var transaction = context.Database.BeginTransaction();

        var savedCount = 0;

        foreach (var row in WorkerRows)
        {
            foreach (var cell in row.Cells)
            {
                if (string.IsNullOrWhiteSpace(cell.DurationHoursText))
                {
                    continue;
                }

                if (!decimal.TryParse(cell.DurationHoursText, out var durationHours))
                {
                    MessageBox.Show($"Please enter a valid duration for {row.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
                    return;
                }

                if (durationHours <= 0)
                {
                    continue;
                }

                if (durationHours > 16)
                {
                    MessageBox.Show($"Duration cannot exceed 16 hours for {row.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
                    return;
                }

                if (cell.SelectedConstructionSiteOption?.Id is not int constructionSiteId)
                {
                    MessageBox.Show($"Please select a construction site for {row.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
                    return;
                }

                var dailyRate = GetDailyRateForDate(context, row.WorkerId, cell.WorkDate);

                if (dailyRate <= 0)
                {
                    MessageBox.Show($"No daily rate was found for {row.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
                    return;
                }

                var existingLog = context.WorkLogs
                    .FirstOrDefault(workLog =>
                        workLog.WorkerId == row.WorkerId &&
                        workLog.WorkDate == cell.WorkDate);

                var hourlyRate = dailyRate / 8m;
                var totalAmount = Math.Round(durationHours * hourlyRate, 2);
                var now = DateTime.Now;

                if (existingLog is null)
                {
                    context.WorkLogs.Add(new WorkLog
                    {
                        WorkerId = row.WorkerId,
                        ConstructionSiteId = constructionSiteId,
                        WorkDate = cell.WorkDate,
                        DurationHours = Math.Round(durationHours, 2),
                        DailyRateSnapshot = dailyRate,
                        TotalAmount = totalAmount,
                        Notes = "Created from weekly work entry.",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                else
                {
                    existingLog.ConstructionSiteId = constructionSiteId;
                    existingLog.DurationHours = Math.Round(durationHours, 2);
                    existingLog.DailyRateSnapshot = dailyRate;
                    existingLog.TotalAmount = totalAmount;
                    existingLog.UpdatedAt = now;
                }

                savedCount++;
            }
        }

        context.SaveChanges();
        transaction.Commit();

        LoadWorkerRows();
        StatusMessage = savedCount == 0
            ? "No weekly entries were changed."
            : $"{savedCount} weekly work entries saved.";

        if (savedCount > 0)
        {
            ToastNotificationService.ShowSuccess(StatusMessage);
        }
    }

    private void LoadTradeOptions()
    {
        using var context = new AppDbContext();
        var selectedTradeId = SelectedTradeOption?.Id;
        var trades = context.Trades
            .AsNoTracking()
            .Where(trade => trade.IsActive)
            .OrderBy(trade => trade.Name)
            .Select(trade => new LookupOption
            {
                Id = trade.Id,
                Name = trade.Name
            })
            .ToList();

        TradeOptions.Clear();

        foreach (var trade in trades)
        {
            TradeOptions.Add(trade);
        }

        SelectedTradeOption = TradeOptions.FirstOrDefault(trade => trade.Id == selectedTradeId)
                              ?? TradeOptions.FirstOrDefault();
    }

    private void LoadWeekDays()
    {
        WeekDays.Clear();

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = WeekStart.AddDays(dayOffset);
            WeekDays.Add(new WeekDayColumn
            {
                Date = date,
                Header = $"{date:ddd}\n{date:MMM dd}"
            });
        }
    }

    private void LoadWorkerRows()
    {
        WorkerRows.Clear();

        if (SelectedTradeOption?.Id is not int tradeId)
        {
            StatusMessage = "Create or select a trade to start weekly entry.";
            return;
        }

        using var context = new AppDbContext();
        var workers = context.Workers
            .AsNoTracking()
            .Include(worker => worker.Trade)
            .Where(worker => worker.TradeId == tradeId && worker.Status == EntityStatus.Active)
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .ToList();

        var workerIds = workers.Select(worker => worker.Id).ToList();
        var existingLogs = context.WorkLogs
            .AsNoTracking()
            .Where(workLog =>
                workerIds.Contains(workLog.WorkerId) &&
                workLog.WorkDate >= WeekStart &&
                workLog.WorkDate <= WeekEnd)
            .ToList();

        foreach (var worker in workers)
        {
            var assignedSites = context.WorkerConstructionSites
                .AsNoTracking()
                .Include(workerSite => workerSite.ConstructionSite)
                .Where(workerSite =>
                    workerSite.WorkerId == worker.Id &&
                    workerSite.Status == EntityStatus.Active &&
                    workerSite.ConstructionSite != null &&
                    workerSite.ConstructionSite.Status == EntityStatus.Active)
                .OrderBy(workerSite => workerSite.ConstructionSite!.Name)
                .Select(workerSite => new LookupOption
                {
                    Id = workerSite.ConstructionSiteId,
                    Name = workerSite.ConstructionSite!.Name
                })
                .ToList();

            var row = new WeeklyWorkerRow
            {
                WorkerId = worker.Id,
                WorkerName = $"{worker.FirstName} {worker.LastName}".Trim()
            };

            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var date = WeekStart.AddDays(dayOffset);
                var existingLog = existingLogs
                    .Where(workLog => workLog.WorkerId == worker.Id && workLog.WorkDate == date)
                    .OrderBy(workLog => workLog.Id)
                    .FirstOrDefault();

                var cell = new WeeklyWorkLogCell
                {
                    WorkerId = worker.Id,
                    WorkDate = date,
                    DurationHoursText = existingLog?.DurationHours.ToString("0.##") ?? string.Empty
                };

                foreach (var site in assignedSites)
                {
                    cell.ConstructionSiteOptions.Add(new LookupOption
                    {
                        Id = site.Id,
                        Name = site.Name
                    });
                }

                cell.SelectedConstructionSiteOption = cell.ConstructionSiteOptions
                    .FirstOrDefault(site => site.Id == existingLog?.ConstructionSiteId);

                row.Cells.Add(cell);
            }

            WorkerRows.Add(row);
        }

        StatusMessage = workers.Count == 0
            ? "No active workers were found for the selected trade."
            : $"{workers.Count} workers loaded for {SelectedTradeOption.Name}.";
    }

    private static decimal GetDailyRateForDate(AppDbContext context, int workerId, DateTime workDate)
    {
        var rate = context.WorkerRateHistories
            .Where(item => item.WorkerId == workerId &&
                           item.EffectiveFrom <= workDate &&
                           (item.EffectiveTo == null || item.EffectiveTo >= workDate))
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        return rate?.DailyRate ?? 0m;
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

public class WeekDayColumn
{
    public DateTime Date { get; set; }
    public string Header { get; set; } = string.Empty;
}

public partial class WeeklyWorkerRow : ObservableObject
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public ObservableCollection<WeeklyWorkLogCell> Cells { get; } = new();
}

public partial class WeeklyWorkLogCell : ObservableObject
{
    public int WorkerId { get; set; }
    public DateTime WorkDate { get; set; }
    public ObservableCollection<LookupOption> ConstructionSiteOptions { get; } = new();

    [ObservableProperty]
    private string durationHoursText = string.Empty;

    [ObservableProperty]
    private LookupOption? selectedConstructionSiteOption;

    [ObservableProperty]
    private bool isReadOnly;
}
