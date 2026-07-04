# Site Workforce Manager — CLAUDE.md

## Project Overview
WPF desktop app (.NET 8) for managing construction site workers: logging hours, tracking daily rates, running payroll, and generating reports. Targets a single client; the app ships as a single self-contained EXE with no pre-existing database.

## Tech Stack
- **.NET 8 WPF**, `UseWPF=true`, `net8.0-windows`
- **MVVM** via `CommunityToolkit.Mvvm` 8.4.x (`[ObservableProperty]`, `[RelayCommand]`, partial void On…Changed)
- **SQLite** via EF Core 8 (`EnsureCreated()` — no migrations; schema managed manually in `DatabaseInitializer`)
- **ClosedXML** for Excel export
- **Printing** via WPF `FlowDocument` / `DocumentPaginator`

## Project Structure
```
Models/          EF Core entities (Worker, Trade, WorkLog, WorkerPayment, WorkerRateHistory, ConstructionSite, …)
Data/            AppDbContext
ViewModels/      One VM per page; MainViewModel owns navigation
Views/           UserControl per page; DataTemplates in MainWindow.xaml
Helpers/         Converters, behaviors, PagedList<T>, PageSizeOptions
Services/        DatabaseInitializer, print services, toast/dialog services
Styles/          Theme.xaml — all global styles and brushes
```

## Build & Run
```powershell
dotnet build
dotnet run
# Publish single-file self-contained EXE:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
No need to ask permission before running PowerShell or build commands — just run them.

## Database
- SQLite file created next to the EXE on first launch via `DatabaseInitializer.Initialize()` → `EnsureCreated()` + manual DDL migrations.
- **No EF migrations.** Schema changes go in `DatabaseInitializer.cs` as raw SQL with `IF NOT EXISTS` / `IF NOT EXISTS column` guards.
- SQLite stores `decimal` as `TEXT`. Always cast in LINQ: `(double)x.Amount` → sum → cast back to `decimal`. Never use `.Sum()` directly on decimal EF props.

## Key Patterns

### Navigation
`MainViewModel` holds one `CurrentViewModel` property. Each page is a VM; `MainWindow.xaml` uses `DataTemplate` keyed on VM type to auto-resolve the view.

### Pagination
`PagedList<T>` (Helpers/PagedList.cs) — generic paged collection with Prev/Next commands, `PageSize` (25/50/100), `PageInfoText`.  
`PageSizeOptions.Values` is a separate static class for `x:Static` binding in XAML (generic types can't be used with `x:Static`).  
Pattern: keep a full filtered `List<T>` for export/totals, call `XxxPage.SetSource(filteredList)` for display.

### Numeric Inputs
`PositiveDecimalBehavior.IsEnabled="True"` (Helpers/PositiveDecimalBehavior.cs) — blocks letters, `e`/`E`, minus/subtract, and non-numeric paste on any TextBox. Apply to all daily rate, hours, and payment fields.

### Printing
RTL Arabic `FlowDocument` printing in `WeeklyReportPrintService` and `PayrollPrintService`. Use proportional column widths: compute `unit = availableWidth / totalWeight` then `colWidth = weight * unit` so columns always sum to the printable area (prevents A4 overflow).

### Formatting
Negative currency values display as parentheses, not minus: `v < 0 ? $"({Math.Abs(v):C})" : v.ToString("C")` — shared via `PayrollFmt.Fmt()`.  
Zero payment totals display as empty string, not `$0.00`.

### Checkbox Style
Global custom `ControlTemplate` in `Theme.xaml` — 16×16, `CornerRadius="4"`, blue fill on check, border highlight on hover. Do not add per-view checkbox styles that conflict.

### Filter Dropdowns (Reports)
ComboBox items for Workers/Categories/Sites use a bare `ContentPresenter` `ItemContainerStyle` (no default chrome) so the entire row is clickable. The checkbox inside has a full-width `Border` as its background for hit-testing.

## Pages / ViewModels

| Page | VM | Notes |
|---|---|---|
| Dashboard | DashboardViewModel | Summary stats |
| Categories | TradesViewModel | CRUD for trades/categories |
| Construction Sites | ConstructionSitesViewModel | CRUD |
| Workers | WorkersViewModel | CRUD + daily rate history |
| Weekly Entry | WeeklyWorkEntryViewModel | Thu–Wed week grid, logs hours per worker/day |
| Payroll | PayrollViewModel | Weekly payment entry grouped by category; negative = parentheses; zero total = blank |
| Reports | ReportsViewModel | 6 tabs (Work Logs, Payments, Worker Summary, Category, Site, Date); auto-filters on change; export targets active tab |
| Weekly Report | WeeklyReportViewModel | Hours/earnings/balance per worker for selected week, mandatory category filter; Arabic RTL print |
| Worker Balances | WorkerBalancesViewModel | Running balance per worker |
| Maintenance | MaintenanceViewModel | Backup DB only (Restore removed) |

## Sidebar Order
Dashboard → Categories → Construction Sites → Workers → Weekly Entry → Payroll → Weekly Report → Reports → Worker Balances → Maintenance

## Weeks
Weeks run **Thursday → Wednesday** throughout the app.

## Balance Calculation
- **Live balance** = sum of all `WorkLog.TotalAmount` for a worker − sum of all `WorkerPayment.Amount` for that worker (no date restriction).
- **Weekly Report balance fields** (all scoped per worker):
  - `BalanceBeforeWeek` = sum(`WorkLog.TotalAmount` where `WorkDate < WeekStart`) − sum(`Payment.Amount` where `WeekStartDate < WeekStart`)
  - `WeekEarnings` = sum(`WorkLog.TotalAmount` where `WorkDate >= WeekStart AND WorkDate <= WeekEnd`)
  - `TotalEarnedUpToWeekEnd` = `BalanceBeforeWeek` + `WeekEarnings`
  - `TotalPaidUpToWeekEnd` = sum(`Payment.Amount` where `WeekStartDate == WeekStart`) — blank if 0
  - `TotalBalanceTillWeekEnd` = `TotalEarnedUpToWeekEnd` − `TotalPaidUpToWeekEnd` — blank if `TotalPaidUpToWeekEnd == 0`
  - `DailyRate` = most recent `WorkerRateHistory.DailyRate` where `EffectiveFrom <= WeekStart`

## Misc Rules
- Do not add `ApplyFilters` buttons — all filters apply on change.
- Do not add error handling for internal impossible states — only validate at UI/DB boundaries.
- No comments unless the WHY is non-obvious.
- No trailing summaries in responses — user can read the diff.
