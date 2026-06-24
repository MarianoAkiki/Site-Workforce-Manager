# Project Overview

Site Workforce Manager is a desktop business application for managing workers, construction sites, work logs, and operational reporting for a construction workforce. It is built as a .NET 8 WPF application using the MVVM pattern, with SQLite for local storage through Entity Framework Core.

# Architecture

- .NET 8
- WPF
- MVVM
- SQLite
- Entity Framework Core
- CommunityToolkit.Mvvm

# Implemented Features

## Dashboard

- Dashboard landing page
- Active workers summary
- Active construction sites summary
- Total hours this month
- Total labor cost this month
- Work logs count

## Trades

- Trade create and update
- Trade list view
- Trade deactivate
- Duplicate trade name prevention
- Inactive trades excluded from new worker selection

## Workers

- Worker create and update
- Worker list view
- Worker deactivate
- Trade selection from master trade list
- Daily rate history tracking
- Add new daily rate with effective date
- View rate history per worker
- Worker to construction site assignment management
- Assigned sites count in worker list

## Worker Rate History

- Effective-dated daily rate records per worker
- Automatic current rate lookup by work date
- Historical rate tracking for reporting
- Rate history UI shows only Rate and Effective From (Effective To managed internally)
- Inline rate value editing: click Edit on any rate row to correct its value
- Correcting a rate automatically bulk-recalculates all work logs in that rate's effective period (updates DailyRateSnapshot and TotalAmount)
- Adding a new rate leaves existing work logs untouched; only new logs from the effective date onward use the new rate

## Construction Sites

- Construction site create and update
- Construction site list view
- Construction site deactivate

## Worker ↔ Construction Site Assignments

- Assign workers to multiple construction sites
- Remove worker-site assignments
- Prevent duplicate assignments
- Prevent inactive worker or inactive site assignment

## Work Logs

- Weekly work entry by trade
- Weekly Entry opens on the current Thursday-to-Wednesday week by default
- Previous and current weekly entries can be edited
- Work logs auto-save when both duration hours and construction site are filled, triggered on field blur (LostFocus)
- If worker has only one assigned construction site it is auto-selected on load
- Create and update work logs from the weekly entry page
- Replace existing worker/date work log when a weekly cell is edited
- Partial weekly entry is supported; the full week does not need to be filled
- Filter work logs by date range
- Filter work logs by worker
- Filter work logs by construction site
- Automatic amount calculation
- Automatic daily rate snapshot based on worker rate history and work date
- Worker-based construction site filtering
- Totals for filtered work logs

## Reporting

- Read-only Reports page
- Filter by date from and date to
- Filter by multiple workers or all workers
- Filter by multiple trades or all trades
- Filter by multiple construction sites or all construction sites
- View matching work logs in a report grid
- Summary by worker
- Summary by trade
- Summary by construction site
- Summary by date
- View total hours
- View total amount

## Payroll Payments

- Weekly payroll/payment page
- Payroll view grouped by trade
- Worker balance shown for every week (earned up to week end minus payments made up to week end)
- Payment amount auto-saves per worker on field blur — no Save button required
- Payments can exceed worker balance (negative balance allowed)
- Payment date recorded as the actual date the payment is made (`DateTime.Today`), not the week end date
- Week association stored via `WeekStartDate` column on `WorkerPayment`
- Previous payroll weeks are read-only; only the latest completed week is editable
- Previous weeks still show paid amount recorded for that week
- Live current balance popup per worker (computed on demand when icon clicked, not on page load)

## Worker Balances

- Dedicated Worker Balances page
- Shows all active workers with total earned, total paid, and current balance
- Balance is all-time: sum of all work logs minus sum of all payments
- Filter by worker name and trade
- Toggle to show only workers with an outstanding balance
- Grand total row for filtered results

## Excel Export

- Export current filtered report results to Excel
- Include detailed report rows and totals

## Backup & Restore

- Maintenance page
- Backup SQLite database to selected file
- Restore SQLite database from selected backup
- Reload application data after restore

## UI Improvements

- Shared light theme in a central ResourceDictionary
- Modern sidebar styling
- Active navigation highlighting with blue accent
- Improved hover states for navigation buttons
- Rounded buttons and cards
- Improved spacing and padding across views
- Cleaner card-style page sections
- Improved DataGrid row height, headers, and alternating row colors
- More prominent totals cards
- Consistent typography, borders, and color palette

# Database Schema

## Entities

- `Trade`
  - `Id`
  - `Name`
  - `Description`
  - `IsActive`
  - `CreatedAt`
  - `UpdatedAt`

- `Worker`
  - `Id`
  - `FirstName`
  - `LastName`
  - `TradeId`
  - `Status`

- `WorkerRateHistory`
  - `Id`
  - `WorkerId`
  - `DailyRate`
  - `EffectiveFrom`
  - `EffectiveTo`

- `ConstructionSite`
  - `Id`
  - `Name`
  - `Location`
  - `Status`

- `WorkerConstructionSite`
  - `WorkerId`
  - `ConstructionSiteId`
  - `AssignedDate`
  - `Status`

- `WorkLog`
  - `Id`
  - `WorkerId`
  - `ConstructionSiteId`
  - `WorkDate`
  - `DurationHours`
  - `DailyRateSnapshot`
  - `TotalAmount`
  - `CreatedAt`
  - `UpdatedAt`

- `WorkerPayment`
  - `Id`
  - `WorkerId`
  - `PaymentDate` — actual date the payment was made
  - `Amount`
  - `WeekStartDate` — identifies which payroll week this payment belongs to
  - `CreatedAt`
  - `UpdatedAt`

## Relationships

- `Trade` 1-to-many `Worker`
- `Worker` 1-to-many `WorkerRateHistory`
- `Worker` 1-to-many `WorkerConstructionSite`
- `ConstructionSite` 1-to-many `WorkerConstructionSite`
- `Worker` 1-to-many `WorkLog`
- `Worker` 1-to-many `WorkerPayment`
- `ConstructionSite` 1-to-many `WorkLog`
- `WorkerConstructionSite` uses a composite key:
  - `WorkerId`
  - `ConstructionSiteId`

# Business Rules

- Worker first name and last name are required
- Trade name is required
- Construction site name and location are required
- New daily rates are added with an effective date
- Existing open worker rate history is closed when a later rate is added
- Worker is required for a work log
- Construction site is required for a work log
- Work date is required for a work log
- Weekly Entry opens on the current Thursday-to-Wednesday week
- Weekly Entry supports editing current and past weeks
- Work log duration is entered in hours on the weekly entry page
- Work logs auto-save only after both duration hours and construction site are filled, on field blur
- If a worker has only one assigned construction site it is auto-selected in the weekly entry grid
- Auto-save updates an existing log for the same worker and work date instead of creating duplicates
- Work log total amount is calculated automatically
- Daily rate is snapshotted on Work Log creation
- Work log total amount uses `DailyRateSnapshot / 8 * DurationHours`
- Work logs cannot be created if no valid daily rate exists for that worker on that date
- Reports are read-only
- Worker balances are calculated from saved work logs and saved worker payments
- Worker balance is not stored as a separate database row
- Weekly payroll payments are saved in `WorkerPayment` with the actual payment date and the payroll week identified by `WeekStartDate`
- Payroll balance for a given week = all work logs up to week end minus all payments with payment date up to week end
- Payment amount can exceed worker balance; negative balance is allowed
- Previous payroll weeks are read-only; only the latest completed week accepts payments
- Worker Balances page shows all-time balance per worker (all logs minus all payments)

# Documentation

- `PROJECT_STATUS.md`: Technical status, implemented features, architecture, schema, and future work.
- `APPLICATION_FLOW.md`: Complete business flow and user workflow documentation.

# Future Enhancements

- Payroll module redesign
- Advanced worker balance history
- PDF Report Export
- Advanced Dashboard Charts
- User Authentication
- Multi-user Support
- Cloud Synchronization
- Angular Web Version
- Mobile Version
- Arabic Language Support

# Known Limitations

- The application currently uses a local SQLite database file intended for single-user desktop usage
- Work logs are entered through a weekly grid rather than individual start/end time records
- Database evolution is currently handled in a simple startup-friendly way rather than through a full migration workflow
- Restore replaces the active database file directly and is intended for local maintenance workflows

# Project Status

Current Phase:
Feature Complete MVP

Remaining Work:

- UI/UX Refinement
- End-to-End Testing
- Bug Fixing
- Deployment Packaging
- Client Acceptance Testing

# Next Development Phase

The next phase should focus on stabilization, usability refinement, and production readiness rather than new core business modules. The main priorities are testing, packaging, UX polish, and validating the full workflow with client scenarios.
