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

## Workers

- Worker create and update
- Worker list view
- Worker deactivate
- Hourly rate history tracking
- Add new hourly rate with effective date
- View rate history per worker

## Construction Sites

- Construction site create and update
- Construction site list view
- Construction site deactivate

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
- Totals for filtered work logs excluding cancelled logs

## Reporting

- Read-only Reports page
- Filter by date from and date to
- Filter by multiple workers or all workers
- Filter by multiple construction sites or all construction sites
- Filter by payment status: All, Paid, Unpaid, Cancelled
- View matching work logs in a report grid
- View total hours
- View total amount
- Cancelled logs are excluded from totals

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

- `Worker`
  - `Id`
  - `FirstName`
  - `LastName`
  - `Trade`
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

## Relationships

- `Worker` 1-to-many `WorkerRateHistory`
- `Worker` 1-to-many `WorkerConstructionSite`
- `ConstructionSite` 1-to-many `WorkerConstructionSite`
- `Worker` 1-to-many `WorkLog`
- `ConstructionSite` 1-to-many `WorkLog`
- `WorkerConstructionSite` uses a composite key:
  - `WorkerId`
  - `ConstructionSiteId`

# Business Rules

- Worker first name, last name, and trade are required
- Construction site name and location are required
- New hourly rates are added with an effective date
- Existing open worker rate history is closed when a later rate is added
- Worker is required for a work log
- Construction site is required for a work log
- Work date is required for a work log
- End time must be after start time
- Work log duration is calculated automatically
- Work log total amount is calculated automatically
- Hourly rate snapshot is taken automatically from `WorkerRateHistory` using the selected work date
- Work logs cannot be created if no valid hourly rate exists for that worker on that date
- New work logs are automatically created with `PaymentStatus = Unpaid`
- Paid work logs cannot be edited
- Cancelled work logs cannot be edited
- Cancelled work logs remain visible in work logs and reports when included by filters
- Cancelled work logs are excluded from totals
- Reports are read-only
- Reports do not change payment status
- Reports do not create payroll slips

# Planned Features

- Payroll Slips
- Worker Balances
- Partial Payments
- Payment History
- PDF Export
- Excel Export
- Backup / Restore

# Known Limitations

- The application currently uses a local SQLite database file intended for single-user desktop usage
- Work log start and end times are entered as text in `HH:mm` format rather than through a dedicated time picker
- Payment status can be filtered and reported, but there is no dedicated payment management workflow yet
- There is no payroll generation, balance tracking, or payment history screen yet
- Reports support filtering and totals, but there is no export feature yet
- Database evolution is currently handled in a simple startup-friendly way rather than through a full migration workflow

# Next Development Phase

The next phase should focus on financial workflow features. A strong next step would be adding payroll slips and worker balances, followed by payment history and partial payment support. After that, reporting can be extended with export options such as PDF and Excel, and the application can be strengthened further with backup and restore capabilities.
