using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private bool isInitializing;

    public ReportsViewModel()
    {
        isInitializing = true;
        LoadLookupData();
        SetDefaultDateRange();
        isInitializing = false;
        ApplyFilters();
    }

    public ObservableCollection<ReportRow> ReportWorkLogs { get; } = new();
    public ObservableCollection<WorkerSummaryRow> WorkerSummaries { get; } = new();
    public ObservableCollection<TradeSummaryRow> TradeSummaries { get; } = new();
    public ObservableCollection<ConstructionSiteSummaryRow> ConstructionSiteSummaries { get; } = new();
    public ObservableCollection<DateSummaryRow> DateSummaries { get; } = new();
    public ObservableCollection<SelectableLookupOption> WorkerOptions { get; } = new();
    public ObservableCollection<SelectableLookupOption> TradeOptions { get; } = new();
    public ObservableCollection<SelectableLookupOption> ConstructionSiteOptions { get; } = new();

    [ObservableProperty]
    private DateTime? dateFrom;

    [ObservableProperty]
    private DateTime? dateTo;

    [ObservableProperty]
    private string workerSearchText = string.Empty;

    [ObservableProperty]
    private string tradeSearchText = string.Empty;

    [ObservableProperty]
    private string constructionSiteSearchText = string.Empty;

    [ObservableProperty]
    private decimal totalHours;

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private int numberOfWorkers;

    [ObservableProperty]
    private int numberOfTrades;

    [ObservableProperty]
    private int numberOfConstructionSites;

    [ObservableProperty]
    private int numberOfLogs;

    [ObservableProperty]
    private bool isExportConfirmationVisible;

    [ObservableProperty]
    private bool isExportInProgress;

    public string WorkerSelectionSummary => BuildSelectionSummary(WorkerOptions, "All workers");
    public string TradeSelectionSummary => BuildSelectionSummary(TradeOptions, "All categories");
    public string ConstructionSiteSelectionSummary => BuildSelectionSummary(ConstructionSiteOptions, "All construction sites");
    public bool HasWorkerFilter => !string.IsNullOrWhiteSpace(WorkerSearchText) || WorkerOptions.Any(option => option.IsSelected);
    public bool HasTradeFilter => !string.IsNullOrWhiteSpace(TradeSearchText) || TradeOptions.Any(option => option.IsSelected);
    public bool HasConstructionSiteFilter => !string.IsNullOrWhiteSpace(ConstructionSiteSearchText) || ConstructionSiteOptions.Any(option => option.IsSelected);
    public bool ShowWorkerSelectionPreview => string.IsNullOrWhiteSpace(WorkerSearchText);
    public bool ShowTradeSelectionPreview => string.IsNullOrWhiteSpace(TradeSearchText);
    public bool ShowConstructionSiteSelectionPreview => string.IsNullOrWhiteSpace(ConstructionSiteSearchText);

    public void LoadReport()
    {
        isInitializing = true;
        LoadLookupData();
        SetDefaultDateRange();
        isInitializing = false;
        ApplyFilters();
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        if (!isInitializing)
        {
            ApplyFilters();
        }
    }

    partial void OnDateToChanged(DateTime? value)
    {
        if (!isInitializing)
        {
            ApplyFilters();
        }
    }

    partial void OnWorkerSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasWorkerFilter));
        OnPropertyChanged(nameof(ShowWorkerSelectionPreview));
    }

    partial void OnTradeSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasTradeFilter));
        OnPropertyChanged(nameof(ShowTradeSelectionPreview));
    }

    partial void OnConstructionSiteSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasConstructionSiteFilter));
        OnPropertyChanged(nameof(ShowConstructionSiteSelectionPreview));
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        IsExportConfirmationVisible = false;
        IsExportInProgress = false;
        using var context = new AppDbContext();

        var selectedWorkerIds = WorkerOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var selectedSiteIds = ConstructionSiteOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var selectedTradeIds = TradeOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToList();

        var query = context.WorkLogs
            .AsNoTracking()
            .Include(workLog => workLog.Worker)
            .ThenInclude(worker => worker!.Trade)
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

        if (selectedTradeIds.Count > 0)
        {
            query = query.Where(workLog => workLog.Worker!.TradeId.HasValue && selectedTradeIds.Contains(workLog.Worker.TradeId.Value));
        }

        if (selectedSiteIds.Count > 0)
        {
            query = query.Where(workLog => selectedSiteIds.Contains(workLog.ConstructionSiteId));
        }

        var workLogs = query
            .OrderByDescending(workLog => workLog.WorkDate)
            .ThenBy(workLog => workLog.Worker!.FirstName)
            .ThenBy(workLog => workLog.Worker!.LastName)
            .ToList();

        ReportWorkLogs.Clear();
        WorkerSummaries.Clear();
        TradeSummaries.Clear();
        ConstructionSiteSummaries.Clear();
        DateSummaries.Clear();

        foreach (var workLog in workLogs)
        {
            ReportWorkLogs.Add(new ReportRow
            {
                Worker = $"{workLog.Worker?.FirstName} {workLog.Worker?.LastName}".Trim(),
                Trade = workLog.Worker?.Trade?.Name ?? "Unassigned",
                ConstructionSite = workLog.ConstructionSite?.Name ?? string.Empty,
                WorkDate = workLog.WorkDate,
                DurationHours = workLog.DurationHours,
                DailyRate = workLog.DailyRateSnapshot,
                TotalAmount = workLog.TotalAmount
            });
        }

        foreach (var summary in workLogs
                     .GroupBy(workLog => workLog.Worker?.Trade?.Name ?? "Unassigned")
                     .OrderBy(group => group.Key))
        {
            TradeSummaries.Add(new TradeSummaryRow
            {
                Trade = summary.Key,
                TotalHours = Math.Round(summary.Sum(item => item.DurationHours), 2),
                TotalAmount = Math.Round(summary.Sum(item => item.TotalAmount), 2),
                NumberOfLogs = summary.Count()
            });
        }

        foreach (var summary in workLogs
                     .GroupBy(workLog => $"{workLog.Worker?.FirstName} {workLog.Worker?.LastName}".Trim())
                     .OrderBy(group => group.Key))
        {
            WorkerSummaries.Add(new WorkerSummaryRow
            {
                Worker = summary.Key,
                TotalHours = Math.Round(summary.Sum(item => item.DurationHours), 2),
                TotalAmount = Math.Round(summary.Sum(item => item.TotalAmount), 2),
                NumberOfLogs = summary.Count()
            });
        }

        foreach (var summary in workLogs
                     .GroupBy(workLog => workLog.ConstructionSite?.Name ?? string.Empty)
                     .OrderBy(group => group.Key))
        {
            ConstructionSiteSummaries.Add(new ConstructionSiteSummaryRow
            {
                ConstructionSite = summary.Key,
                TotalHours = Math.Round(summary.Sum(item => item.DurationHours), 2),
                TotalAmount = Math.Round(summary.Sum(item => item.TotalAmount), 2),
                NumberOfLogs = summary.Count()
            });
        }

        foreach (var summary in workLogs
                     .GroupBy(workLog => workLog.WorkDate.Date)
                     .OrderBy(group => group.Key))
        {
            DateSummaries.Add(new DateSummaryRow
            {
                WorkDate = summary.Key,
                TotalHours = Math.Round(summary.Sum(item => item.DurationHours), 2),
                TotalAmount = Math.Round(summary.Sum(item => item.TotalAmount), 2),
                NumberOfLogs = summary.Count()
            });
        }

        TotalHours = Math.Round(workLogs.Sum(workLog => workLog.DurationHours), 2);
        TotalAmount = Math.Round(workLogs.Sum(workLog => workLog.TotalAmount), 2);
        NumberOfWorkers = workLogs
            .Select(workLog => workLog.WorkerId)
            .Distinct()
            .Count();
        NumberOfTrades = workLogs
            .Where(workLog => workLog.Worker?.TradeId.HasValue == true)
            .Select(workLog => workLog.Worker!.TradeId!.Value)
            .Distinct()
            .Count();
        NumberOfConstructionSites = workLogs
            .Select(workLog => workLog.ConstructionSiteId)
            .Distinct()
            .Count();
        NumberOfLogs = workLogs.Count;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        IsExportConfirmationVisible = false;
        IsExportInProgress = false;
        DateFrom = null;
        DateTo = null;
        WorkerSearchText = string.Empty;
        TradeSearchText = string.Empty;
        ConstructionSiteSearchText = string.Empty;
        foreach (var option in WorkerOptions)
        {
            option.IsSelected = false;
        }

        foreach (var option in TradeOptions)
        {
            option.IsSelected = false;
        }

        foreach (var option in ConstructionSiteOptions)
        {
            option.IsSelected = false;
        }

        OnPropertyChanged(nameof(WorkerSelectionSummary));
        OnPropertyChanged(nameof(TradeSelectionSummary));
        OnPropertyChanged(nameof(ConstructionSiteSelectionSummary));
        OnPropertyChanged(nameof(HasWorkerFilter));
        OnPropertyChanged(nameof(HasTradeFilter));
        OnPropertyChanged(nameof(HasConstructionSiteFilter));
    }

    [RelayCommand]
    private void ClearWorkerFilter()
    {
        WorkerSearchText = string.Empty;
        ClearSelectedOptions(WorkerOptions);
        OnPropertyChanged(nameof(WorkerSelectionSummary));
        OnPropertyChanged(nameof(HasWorkerFilter));
        ApplyFilters();
    }

    [RelayCommand]
    private void ClearTradeFilter()
    {
        TradeSearchText = string.Empty;
        ClearSelectedOptions(TradeOptions);
        OnPropertyChanged(nameof(TradeSelectionSummary));
        OnPropertyChanged(nameof(HasTradeFilter));
        ApplyFilters();
    }

    [RelayCommand]
    private void ClearConstructionSiteFilter()
    {
        ConstructionSiteSearchText = string.Empty;
        ClearSelectedOptions(ConstructionSiteOptions);
        OnPropertyChanged(nameof(ConstructionSiteSelectionSummary));
        OnPropertyChanged(nameof(HasConstructionSiteFilter));
        ApplyFilters();
    }

    [RelayCommand]
    private void RequestExportToExcel()
    {
        if (ReportWorkLogs.Count == 0)
        {
            MessageBox.Show("There are no report rows to export.");
            return;
        }

        IsExportConfirmationVisible = true;
    }

    [RelayCommand]
    private void CancelExportToExcel()
    {
        IsExportConfirmationVisible = false;
    }

    [RelayCommand]
    private async Task ConfirmExportToExcel()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Reports to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"reports-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            IsExportConfirmationVisible = false;
            IsExportInProgress = true;
            await Task.Yield();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report Results");

            var headers = new[]
            {
                "Worker",
                "Category",
                "Construction Site",
                "Work Date",
                "Duration Hours",
                "Daily Rate",
                "Total Amount"
            };

            for (var column = 0; column < headers.Length; column++)
            {
                var cell = worksheet.Cell(1, column + 1);
                cell.Value = headers[column];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0EAFF");
            }

            var rowIndex = 2;

            foreach (var row in ReportWorkLogs)
            {
                worksheet.Cell(rowIndex, 1).Value = row.Worker;
                worksheet.Cell(rowIndex, 2).Value = row.Trade;
                worksheet.Cell(rowIndex, 3).Value = row.ConstructionSite;
                worksheet.Cell(rowIndex, 4).Value = row.WorkDate;
                worksheet.Cell(rowIndex, 4).Style.DateFormat.Format = "yyyy-mm-dd";
                worksheet.Cell(rowIndex, 5).Value = row.DurationHours;
                worksheet.Cell(rowIndex, 6).Value = row.DailyRate;
                worksheet.Cell(rowIndex, 7).Value = row.TotalAmount;
                rowIndex++;
            }

            worksheet.Cell(rowIndex + 1, 1).Value = "Total Hours";
            worksheet.Cell(rowIndex + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(rowIndex + 1, 2).Value = TotalHours;
            worksheet.Cell(rowIndex + 2, 1).Value = "Total Amount";
            worksheet.Cell(rowIndex + 2, 1).Style.Font.Bold = true;
            worksheet.Cell(rowIndex + 2, 2).Value = TotalAmount;

            worksheet.Column(5).Style.NumberFormat.Format = "0.00";
            worksheet.Column(6).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Column(7).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(rowIndex + 1, 2).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(rowIndex + 2, 2).Style.NumberFormat.Format = "$#,##0.00";

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            var destinationDirectory = Path.GetDirectoryName(dialog.FileName);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await Task.Run(() => workbook.SaveAs(dialog.FileName));
            IsExportInProgress = false;
            MessageBox.Show("Report exported to Excel successfully.");
        }
        catch (Exception ex)
        {
            IsExportConfirmationVisible = false;
            IsExportInProgress = false;
            MessageBox.Show($"Export failed: {ex.Message}");
        }
    }

    private void SetDefaultDateRange()
    {
        var weekStart = GetLatestFullWeekStart(DateTime.Today);
        DateFrom = weekStart;
        DateTo   = weekStart.AddDays(6);
    }

    private static DateTime GetLatestFullWeekStart(DateTime today)
    {
        var daysSinceWednesday = ((int)today.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;
        var lastCompletedWednesday = today.Date.AddDays(-daysSinceWednesday);
        return lastCompletedWednesday.AddDays(-6);
    }

    private void LoadLookupData()
    {
        using var context = new AppDbContext();
        var selectedWorkerIds = WorkerOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToHashSet();
        var selectedTradeIds = TradeOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToHashSet();
        var selectedSiteIds = ConstructionSiteOptions
            .Where(option => option.IsSelected && option.Id.HasValue)
            .Select(option => option.Id!.Value)
            .ToHashSet();

        WorkerOptions.Clear();
        TradeOptions.Clear();
        ConstructionSiteOptions.Clear();

        var workers = context.Workers
            .AsNoTracking()
            .Where(worker => worker.Status == EntityStatus.Active)
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new SelectableLookupOption
            {
                Id = worker.Id,
                Name = $"{worker.FirstName} {worker.LastName}",
                IsSelected = selectedWorkerIds.Contains(worker.Id)
            })
            .ToList();

        var trades = context.Trades
            .AsNoTracking()
            .Where(trade => trade.IsActive)
            .OrderBy(trade => trade.Name)
            .Select(trade => new SelectableLookupOption
            {
                Id = trade.Id,
                Name = trade.Name,
                IsSelected = selectedTradeIds.Contains(trade.Id)
            })
            .ToList();

        var sites = context.ConstructionSites
            .AsNoTracking()
            .Where(site => site.Status == EntityStatus.Active)
            .OrderBy(site => site.Name)
            .Select(site => new SelectableLookupOption
            {
                Id = site.Id,
                Name = site.Name,
                IsSelected = selectedSiteIds.Contains(site.Id)
            })
            .ToList();

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

        foreach (var site in sites)
        {
            site.PropertyChanged += OnFilterOptionPropertyChanged;
            ConstructionSiteOptions.Add(site);
        }

        OnPropertyChanged(nameof(WorkerSelectionSummary));
        OnPropertyChanged(nameof(TradeSelectionSummary));
        OnPropertyChanged(nameof(ConstructionSiteSelectionSummary));
        OnPropertyChanged(nameof(HasWorkerFilter));
        OnPropertyChanged(nameof(HasTradeFilter));
        OnPropertyChanged(nameof(HasConstructionSiteFilter));
    }

    public ReportExportData BuildExportData()
    {
        return new ReportExportData
        {
            DetailedLogs = ReportWorkLogs.ToList(),
            WorkerSummaries = WorkerSummaries.ToList(),
            TradeSummaries = TradeSummaries.ToList(),
            ConstructionSiteSummaries = ConstructionSiteSummaries.ToList(),
            DateSummaries = DateSummaries.ToList(),
            TotalHours = TotalHours,
            TotalAmount = TotalAmount,
            NumberOfWorkers = NumberOfWorkers,
            NumberOfTrades = NumberOfTrades,
            NumberOfConstructionSites = NumberOfConstructionSites,
            NumberOfLogs = NumberOfLogs
        };
    }

    private void OnFilterOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableLookupOption.IsSelected) && !isInitializing)
        {
            OnPropertyChanged(nameof(WorkerSelectionSummary));
            OnPropertyChanged(nameof(TradeSelectionSummary));
            OnPropertyChanged(nameof(ConstructionSiteSelectionSummary));
            OnPropertyChanged(nameof(HasWorkerFilter));
            OnPropertyChanged(nameof(HasTradeFilter));
            OnPropertyChanged(nameof(HasConstructionSiteFilter));
            ApplyFilters();
        }
    }

    private void ClearSelectedOptions(IEnumerable<SelectableLookupOption> options)
    {
        isInitializing = true;

        foreach (var option in options)
        {
            option.IsSelected = false;
        }

        isInitializing = false;
    }

    private static string BuildSelectionSummary(IEnumerable<SelectableLookupOption> options, string allText)
    {
        var selectedNames = options
            .Where(option => option.IsSelected)
            .Select(option => option.Name)
            .ToList();

        if (selectedNames.Count == 0)
        {
            return allText;
        }

        if (selectedNames.Count <= 3)
        {
            return string.Join(", ", selectedNames);
        }

        return $"{string.Join(", ", selectedNames.Take(3))}, +{selectedNames.Count - 3} more";
    }

    public class ReportRow
    {
        public string Worker { get; set; } = string.Empty;
        public string Trade { get; set; } = string.Empty;
        public string ConstructionSite { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public decimal DurationHours { get; set; }
        public decimal DailyRate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class WorkerSummaryRow
    {
        public string Worker { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfLogs { get; set; }
    }

    public class TradeSummaryRow
    {
        public string Trade { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfLogs { get; set; }
    }

    public class ConstructionSiteSummaryRow
    {
        public string ConstructionSite { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfLogs { get; set; }
    }

    public class DateSummaryRow
    {
        public DateTime WorkDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfLogs { get; set; }
    }

    public class ReportExportData
    {
        public List<ReportRow> DetailedLogs { get; set; } = new();
        public List<WorkerSummaryRow> WorkerSummaries { get; set; } = new();
        public List<TradeSummaryRow> TradeSummaries { get; set; } = new();
        public List<ConstructionSiteSummaryRow> ConstructionSiteSummaries { get; set; } = new();
        public List<DateSummaryRow> DateSummaries { get; set; } = new();
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfWorkers { get; set; }
        public int NumberOfTrades { get; set; }
        public int NumberOfConstructionSites { get; set; }
        public int NumberOfLogs { get; set; }
    }
}
