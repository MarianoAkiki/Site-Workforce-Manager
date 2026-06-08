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
- Total outstanding balance
- Unpaid work logs count

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
- Hourly rate history tracking
- Add new hourly rate with effective date
- View rate history per worker
- Worker to construction site assignment management
- Assigned sites count in worker list

## Worker Rate History

- Effective-dated hourly rate records per worker
- Automatic current rate lookup by work date
- Historical rate tracking for payroll and reporting

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

- Create work log
- Edit unpaid work log
- Cancel work log
- Filter work logs by date range
- Filter work logs by worker
- Filter work logs by construction site
- Filter work logs by payment status
- Automatic hour calculation from start and end time
- Automatic amount calculation
- Automatic hourly rate snapshot based on worker rate history and work date
- Payment status tracking
- Worker-based construction site filtering
- Totals for filtered work logs excluding cancelled logs

## Reporting

- Read-only Reports page
- Filter by date from and date to
- Filter by multiple workers or all workers
- Filter by multiple trades or all trades
- Filter by multiple construction sites or all construction sites
- Filter by payment status: All, Paid, Unpaid, Cancelled
- View matching work logs in a report grid
- Summary by worker
- Summary by trade
- Summary by construction site
- Summary by date
- View total hours
- View total amount
- Cancelled logs are excluded from totals

## Payroll Slips

- Generate payroll slips from unpaid work logs
- Worker and date-range payroll filtering
- Snapshot payroll slip lines for historical accuracy
- Slip history view
- Slip detail view with included logs

## Payroll Payments

- Initial payment on payroll slip creation
- Follow-up payments on partially paid slips
- Automatic recalculation of amount paid and remaining balance
- Automatic slip status update to Paid or PartiallyPaid

## Worker Balances

- Worker balance summary page
- Balance filters by worker, trade, date range, and balance status
- Worker-level payroll slip history view
- Worker-level payment history view
- Outstanding balance totals based on payroll slips and payments

## Payroll Cancellation

- Cancel payroll slips with no payments
- Return cancelled slip work logs to Unpaid
- Block direct cancellation when payments exist
- Cancelled payroll slips remain visible for audit history

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
  - `HourlyRate`
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
  - `StartTime`
  - `EndTime`
  - `DurationHours`
  - `HourlyRateSnapshot`
  - `TotalAmount`
  - `PaymentStatus`
  - `Notes`
  - `CreatedAt`
  - `UpdatedAt`

- `PayrollSlip`
  - `Id`
  - `SlipNumber`
  - `WorkerId`
  - `DateFrom`
  - `DateTo`
  - `TotalHours`
  - `TotalAmount`
  - `AmountPaid`
  - `RemainingBalance`
  - `Status`
  - `CreatedAt`
  - `Notes`

- `PayrollSlipLine`
  - `Id`
  - `PayrollSlipId`
  - `WorkLogId`
  - `WorkerNameSnapshot`
  - `TradeNameSnapshot`
  - `ConstructionSiteNameSnapshot`
  - `WorkDate`
  - `StartTime`
  - `EndTime`
  - `DurationHours`
  - `HourlyRateSnapshot`
  - `TotalAmountSnapshot`

- `PayrollPayment`
  - `Id`
  - `PayrollSlipId`
  - `PaymentDate`
  - `Amount`
  - `Notes`

## Relationships

- `Trade` 1-to-many `Worker`
- `Worker` 1-to-many `WorkerRateHistory`
- `Worker` 1-to-many `WorkerConstructionSite`
- `ConstructionSite` 1-to-many `WorkerConstructionSite`
- `Worker` 1-to-many `WorkLog`
- `ConstructionSite` 1-to-many `WorkLog`
- `Worker` 1-to-many `PayrollSlip`
- `PayrollSlip` 1-to-many `PayrollSlipLine`
- `WorkLog` 1-to-many `PayrollSlipLine`
- `PayrollSlip` 1-to-many `PayrollPayment`
- `WorkerConstructionSite` uses a composite key:
  - `WorkerId`
  - `ConstructionSiteId`

# Business Rules

- Worker first name and last name are required
- Trade name is required
- Construction site name and location are required
- New hourly rates are added with an effective date
- Existing open worker rate history is closed when a later rate is added
- Worker is required for a work log
- Construction site is required for a work log
- Work date is required for a work log
- End time must be after start time
- Work log duration is calculated automatically
- Work log total amount is calculated automatically
- Work Logs are always created as Unpaid
- Users cannot manually mark logs as Paid
- Paid status is controlled by Payroll processing
- Hourly rate is snapshotted on Work Log creation
- Work logs cannot be created if no valid hourly rate exists for that worker on that date
- Paid work logs cannot be edited
- Cancelled work logs cannot be edited
- Cancelled work logs remain visible in work logs and reports when included by filters
- Cancelled work logs are excluded from totals
- Payroll slips create historical snapshots
- Work Logs cannot be paid twice
- Worker balances are calculated from Payroll Slips and Payments
- Cancelled records remain visible for audit history
- Historical payroll records must remain unchanged
- Reports are read-only
- Reports do not change payment status
- Payroll slips cannot be deleted

# Documentation

- `PROJECT_STATUS.md`: Technical status, implemented features, architecture, schema, and future work.
- `APPLICATION_FLOW.md`: Complete business flow and user workflow documentation.

# Future Enhancements

- PDF Payroll Slip Export
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
- Work log start and end times are entered as text in `HH:mm` format rather than through a dedicated time picker
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
