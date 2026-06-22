using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class WeeklyWorkEntryViewModel : ObservableObject
{
    private readonly List<WeeklyWorkerRow> allWorkerRows = new();

    public WeeklyWorkEntryViewModel()
    {
        WeekStart = GetCurrentWeekStart(DateTime.Today);
        LoadWeeklyEntryPage();
    }

    public ObservableCollection<LookupOption> TradeOptions { get; } = new();
    public ObservableCollection<WeekDayColumn> WeekDays { get; } = new();
    public ObservableCollection<WeeklyWorkerRow> WorkerRows { get; } = new();
    public ObservableCollection<WeeklyWorkerRow> FilteredWorkerRows { get; } = new();

    [ObservableProperty]
    private LookupOption? selectedTradeOption;

    [ObservableProperty]
    private DateTime weekStart;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string workerIdFilterText = string.Empty;

    [ObservableProperty]
    private string workerNameFilterText = string.Empty;

    public string WeekRangeText => $"{WeekStart:dddd, MMM dd, yyyy} - {WeekEnd:dddd, MMM dd, yyyy}";
    public DateTime WeekEnd => WeekStart.AddDays(6);
    public bool CanGoNextWeek => WeekStart < GetCurrentWeekStart(DateTime.Today);

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

    partial void OnWorkerIdFilterTextChanged(string value)
    {
        RefreshFilteredWorkerRows();
    }

    partial void OnWorkerNameFilterTextChanged(string value)
    {
        RefreshFilteredWorkerRows();
    }

    public void LoadWeeklyEntryPage()
    {
        WorkerIdFilterText = string.Empty;
        WorkerNameFilterText = string.Empty;
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
        allWorkerRows.Clear();
        WorkerRows.Clear();
        FilteredWorkerRows.Clear();

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

        var assignedSitesByWorker = context.WorkerConstructionSites
            .AsNoTracking()
            .Include(workerSite => workerSite.ConstructionSite)
            .Where(workerSite =>
                workerIds.Contains(workerSite.WorkerId) &&
                workerSite.Status == EntityStatus.Active &&
                workerSite.ConstructionSite != null &&
                workerSite.ConstructionSite.Status == EntityStatus.Active)
            .OrderBy(workerSite => workerSite.ConstructionSite!.Name)
            .ToList()
            .GroupBy(workerSite => workerSite.WorkerId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(workerSite => new LookupOption
                {
                    Id = workerSite.ConstructionSiteId,
                    Name = workerSite.ConstructionSite!.Name
                }).ToList());

        foreach (var worker in workers)
        {
            var assignedSites = assignedSitesByWorker.TryGetValue(worker.Id, out var sites) ? sites : [];

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
                    WorkerName = row.WorkerName,
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
                    .FirstOrDefault(site => site.Id == existingLog?.ConstructionSiteId)
                    ?? (cell.ConstructionSiteOptions.Count == 1 ? cell.ConstructionSiteOptions[0] : null);
                cell.AutoSaveRequested = AutoSaveCell;

                row.Cells.Add(cell);
            }

            allWorkerRows.Add(row);
            WorkerRows.Add(row);
        }

        RefreshFilteredWorkerRows();

        if (workers.Count == 0)
        {
            StatusMessage = "No active workers were found for the selected trade.";
        }
        else
        {
            StatusMessage = $"{workers.Count} workers loaded for {SelectedTradeOption.Name}. Entries auto-save when hours and site are filled.";
        }
    }

    private void AutoSaveCell(WeeklyWorkLogCell cell)
    {
        if (string.IsNullOrWhiteSpace(cell.DurationHoursText) ||
            cell.SelectedConstructionSiteOption?.Id is not int constructionSiteId)
        {
            return;
        }

        if (!decimal.TryParse(cell.DurationHoursText, NumberStyles.Number, CultureInfo.InvariantCulture, out var durationHours))
        {
            ShowAutoSaveError($"Please enter a valid duration for {cell.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
            return;
        }

        if (durationHours <= 0)
        {
            ShowAutoSaveError($"Duration must be greater than zero for {cell.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
            return;
        }

        if (durationHours > 16)
        {
            ShowAutoSaveError($"Duration cannot exceed 16 hours for {cell.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
            return;
        }

        using var context = new AppDbContext();
        var dailyRate = GetDailyRateForDate(context, cell.WorkerId, cell.WorkDate);

        if (dailyRate <= 0)
        {
            ShowAutoSaveError($"No daily rate was found for {cell.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.");
            return;
        }

        var existingLog = context.WorkLogs
            .FirstOrDefault(workLog =>
                workLog.WorkerId == cell.WorkerId &&
                workLog.WorkDate == cell.WorkDate);

        var hourlyRate = dailyRate / 8m;
        var totalAmount = Math.Round(durationHours * hourlyRate, 2);
        var now = DateTime.Now;

        if (existingLog is null)
        {
            context.WorkLogs.Add(new WorkLog
            {
                WorkerId = cell.WorkerId,
                ConstructionSiteId = constructionSiteId,
                WorkDate = cell.WorkDate,
                DurationHours = Math.Round(durationHours, 2),
                DailyRateSnapshot = dailyRate,
                TotalAmount = totalAmount,
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

        context.SaveChanges();
        StatusMessage = $"Saved {cell.WorkerName} on {cell.WorkDate:yyyy-MM-dd}.";
    }

    private static void ShowAutoSaveError(string message)
    {
        MessageBox.Show(message, "Weekly Entry Auto-Save", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private void RefreshFilteredWorkerRows()
    {
        FilteredWorkerRows.Clear();

        foreach (var row in allWorkerRows.Where(MatchesWorkerFilter))
        {
            FilteredWorkerRows.Add(row);
        }
    }

    private bool MatchesWorkerFilter(WeeklyWorkerRow row)
    {
        var idFilter = WorkerIdFilterText.Trim();
        var nameFilter = WorkerNameFilterText.Trim();

        var matchesId = string.IsNullOrWhiteSpace(idFilter) ||
                        row.WorkerId.ToString().Contains(idFilter, StringComparison.CurrentCultureIgnoreCase);
        var matchesName = string.IsNullOrWhiteSpace(nameFilter) ||
                          row.WorkerName.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase);

        return matchesId && matchesName;
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

    private static DateTime GetCurrentWeekStart(DateTime today)
    {
        var daysSinceThursday = ((int)today.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return today.Date.AddDays(-daysSinceThursday);
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
    public string WorkerName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public Action<WeeklyWorkLogCell>? AutoSaveRequested { get; set; }
    public ObservableCollection<LookupOption> ConstructionSiteOptions { get; } = new();

    [ObservableProperty]
    private string durationHoursText = string.Empty;

    [ObservableProperty]
    private LookupOption? selectedConstructionSiteOption;

    [ObservableProperty]
    private bool isReadOnly;

    public bool IsEditable => !IsReadOnly;

    partial void OnDurationHoursTextChanged(string value)
    {
        AutoSaveRequested?.Invoke(this);
    }

    partial void OnSelectedConstructionSiteOptionChanged(LookupOption? value)
    {
        AutoSaveRequested?.Invoke(this);
    }

    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEditable));
    }
}
