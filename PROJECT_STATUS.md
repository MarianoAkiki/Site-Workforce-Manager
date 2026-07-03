# Project Overview

Site Workforce Manager is a desktop business application for managing workers, construction sites, work logs, and operational reporting for a construction workforce. It is built as a .NET 8 WPF application using the MVVM pattern, with SQLite for local storage through Entity Framework Core.

# Architecture

- .NET 8
- WPF
- MVVM (CommunityToolkit.Mvvm)
- SQLite via Entity Framework Core (`EnsureCreated()` + manual DDL migrations — no EF migrations)
- ClosedXML for Excel export
- WPF FlowDocument for Arabic RTL printing

# Implemented Features

## Dashboard

- Dashboard landing page
- Active workers summary
- Active construction sites summary
- Total hours this month
- Total labor cost this month
- Work logs count

## Trades (Categories)

- Trade create and update
- Trade list view with pagination (25/50/100 rows)
- Trade deactivate
- Duplicate trade name prevention
- Inactive trades excluded from new worker selection

## Workers

- Worker create and update
- Worker list view with pagination (25/50/100 rows)
- Worker deactivate
- Trade selection from master trade list
- Daily rate history tracking
- Add new daily rate with effective date
- Inline rate value editing: click Edit on any rate row to correct its value
- Correcting a rate automatically bulk-recalculates all work logs in that rate's effective period
- View rate history per worker
- Worker to construction site assignment management
- Assigned sites count in worker list

## Construction Sites

- Construction site create and update
- Construction site list view with pagination (25/50/100 rows)
- Construction site deactivate

## Worker ↔ Construction Site Assignments

- Assign workers to multiple construction sites
- Remove worker-site assignments
- Prevent duplicate assignments
- Prevent inactive worker or inactive site assignment

## Work Logs (Weekly Entry)

- Weekly work entry by trade
- Weekly Entry opens on the current Thursday-to-Wednesday week by default
- Previous and current weekly entries can be edited
- Work logs auto-save when both duration hours and construction site are filled, on field blur (LostFocus)
- If worker has only one assigned construction site it is auto-selected on load
- Create and update work logs from the weekly entry page
- Replace existing worker/date work log when a weekly cell is edited
- Partial weekly entry is supported

## Reporting (Reports)

- Read-only Reports page with 6 tabs:
  - Work Logs (detailed rows)
  - Payments
  - Summary by Worker
  - Summary by Trade (Category)
  - Summary by Construction Site
  - Summary by Date
- Filter by date range, multiple workers, multiple trades, multiple construction sites
- Filters auto-apply on change — no Apply button
- Filter dropdowns: entire row is clickable (bare ContentPresenter item template)
- Pagination on all 6 tabs (25/50/100 rows, default 25)
- Stat strip: Total Hours, Total Earned, Total Paid
- Export to Excel exports the currently active tab's data

## Payroll

- Weekly payroll/payment page (Thu–Wed weeks)
- Grouped by trade/category
- Worker balance shown for every week (earned up to week end minus payments dated up to week end)
- Payment amount auto-saves per worker on field blur — no Save button
- Previous payroll weeks are read-only; only the latest completed week is editable
- Previous weeks show the paid amount recorded for that week
- Live current balance popup per worker (computed on demand, not on page load)
- Negative values shown as parentheses, e.g. (100.00) instead of -100.00
- Category total payment shows blank when no payments entered yet (zero)

## Weekly Report

- Dedicated Weekly Report page
- Mandatory filter by category (trade) — always required
- Shows the selected Thursday-to-Wednesday week (week navigation with < / > arrows)
- Table columns: ID, Name, 7 daily hours, Total Hours, Days, Daily Rate, Balance Before Week, Week Earnings, Total Earned to Week End, Total Paid to Week End, Balance to Week End
- Totals row at the bottom
- Worker count excludes the totals row
- Negative values shown as parentheses
- Arabic RTL print output (A4 landscape, proportional column widths, signature column التوقيع, category name in title)
- Pagination (25/50/100 rows)

## Worker Balances

- Dedicated Worker Balances page with pagination (25/50/100 rows)
- Shows all active workers with total earned, total paid, and current balance
- Balance is all-time: sum of all work logs minus sum of all payments
- Filter by worker ID, worker name, trade
- Toggle to show only workers with an outstanding balance
- Grand total row for filtered results (grand totals computed from full filtered set, not just visible page)

## Maintenance

- Backup SQLite database to selected file
- (Restore removed — backup only)

## UI

- Shared light theme (Styles/Theme.xaml)
- Sidebar with emoji icons; active page highlighted
- Sidebar order: Dashboard → Categories → Construction Sites → Workers → Weekly Entry → Payroll → Weekly Report → Reports → Worker Balances → Maintenance
- Global custom CheckBox style: 16×16, rounded (CornerRadius 4), blue fill on check, smooth hover
- Positive-only numeric inputs: daily rate, hours, and payment fields block letters, `e`, minus, and non-numeric paste
- Negative currency values displayed as parentheses throughout

# Database Schema

## Entities

- `Trade` — Id, Name, Description, IsActive, CreatedAt, UpdatedAt
- `Worker` — Id, FirstName, LastName, TradeId, Status
- `WorkerRateHistory` — Id, WorkerId, DailyRate (TEXT), EffectiveFrom, EffectiveTo
- `ConstructionSite` — Id, Name, Location, Status
- `WorkerConstructionSite` — WorkerId, ConstructionSiteId, AssignedDate, Status (composite PK)
- `WorkLog` — Id, WorkerId, ConstructionSiteId, WorkDate, DurationHours (TEXT), DailyRateSnapshot (TEXT), TotalAmount (TEXT), CreatedAt, UpdatedAt
- `WorkerPayment` — Id, WorkerId, PaymentDate, Amount (TEXT), WeekStartDate, CreatedAt, UpdatedAt

> SQLite stores `decimal` as `TEXT`. Always cast `(double)` before `.Sum()` in LINQ then cast back to `decimal`.

## Relationships

- Trade 1-to-many Worker
- Worker 1-to-many WorkerRateHistory
- Worker 1-to-many WorkerConstructionSite
- ConstructionSite 1-to-many WorkerConstructionSite
- Worker 1-to-many WorkLog
- Worker 1-to-many WorkerPayment
- ConstructionSite 1-to-many WorkLog

# Business Rules

- Worker first name, last name, and trade are required
- Trade name is required and must be unique
- Construction site name and location are required
- New daily rates are added with an effective date; existing open rate is closed automatically
- Weekly Entry opens on the current Thursday-to-Wednesday week
- Work log auto-saves when both duration hours and site are filled, on blur
- Work log total = DailyRateSnapshot / 8 × DurationHours
- Work logs cannot be created if no valid daily rate exists for that worker on that date
- Reports are read-only
- Payroll balance for a week = all work logs with WorkDate ≤ WeekEnd minus all payments with PaymentDate ≤ WeekEnd
- Payment date = actual date payment is made (not week end date)
- WeekStartDate on WorkerPayment identifies the payroll week
- Payment amount can exceed worker balance; negative balance is allowed
- Previous payroll weeks are read-only
- Worker Balances = all-time earnings minus all-time payments
- Weekly Report is always filtered by a mandatory category

# Known Limitations

- Single-user local SQLite database
- Work logs entered through weekly grid (no individual time-of-day tracking)
- Database evolution handled via startup DDL guards, not EF migrations
- Restore removed from Maintenance; client manages backups manually

# Project Status

Current Phase: Feature Complete MVP — deployed as single self-contained EXE, no pre-existing database.

# Documentation

- `PROJECT_STATUS.md` — Technical status, features, schema, business rules.
- `APPLICATION_FLOW.md` — Complete business flow and user workflow.
- `CLAUDE.md` — Developer reference for AI-assisted development (patterns, pitfalls, conventions).
