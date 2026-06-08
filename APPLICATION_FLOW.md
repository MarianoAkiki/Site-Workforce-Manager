# Site Workforce Manager - Application Flow

## Purpose

This document explains the full business flow of the Site Workforce Manager application from setup to daily usage, reporting, payroll, worker balances, cancellation, and backup.

---

## 1. Dashboard

The Dashboard is the application home page.

It provides a quick overview of the business status, including:

* Active Workers
* Active Construction Sites
* Total Hours This Month
* Total Labor Cost This Month
* Outstanding Balances
* Unpaid Work Logs Count

The Dashboard is used for quick monitoring and does not modify data.

---

## 2. Manage Trades

Trades represent worker job types.

Examples:

* Mason
* Electrician
* Plumber
* Painter
* Carpenter

The user can:

* Add trades
* Edit trades
* Deactivate trades

Inactive trades should not be assigned to new workers, but existing workers using inactive trades should still display their trade history correctly.

---

## 3. Manage Workers

The user creates and manages workers.

Worker information includes:

* Worker Name
* Trade
* Status
* Notes
* Hourly Rate History

A worker must be linked to a trade.

Workers can be activated or deactivated.

---

## 4. Manage Worker Hourly Rates

Each worker can have hourly rate history.

When a worker's hourly rate changes:

* The old rate is not overwritten.
* A new rate record is created.
* The new rate has an effective date.
* Historical records remain accurate.

When a work log is created, the system uses the worker rate that is valid for the selected work date.

The selected rate is saved as a snapshot inside the work log.

---

## 5. Manage Construction Sites

The user creates and manages construction sites.

Construction site information includes:

* Site Name
* Location or Description
* Status
* Notes

Construction sites can be activated or deactivated.

Inactive sites should not be used for new work logs.

---

## 6. Assign Workers to Construction Sites

Workers can be assigned to one or multiple construction sites.

A construction site can also have multiple assigned workers.

Example:

Ahmad can be assigned to:

* Site A
* Site B

Business rules:

* Duplicate assignments are not allowed.
* Inactive workers cannot be assigned.
* Inactive construction sites cannot be assigned.
* Work Log site selection may be restricted to sites assigned to the selected worker.

---

## 7. Create Work Logs

Work Logs represent daily work performed by workers.

For each work log, the user selects:

* Worker
* Construction Site
* Work Date
* Start Time
* End Time
* Notes, if needed

The system automatically calculates:

* Duration Hours
* Hourly Rate Snapshot
* Total Amount

Formula:

Duration Hours = End Time - Start Time

Total Amount = Duration Hours × Hourly Rate Snapshot

Business rules:

* End Time must be after Start Time.
* Worker is required.
* Construction Site is required.
* Work Logs are always created as Unpaid.
* Users cannot manually mark Work Logs as Paid.
* Payment status is controlled only by payroll processing.
* Paid Work Logs should not be directly edited.
* Cancelled Work Logs remain visible for history but are excluded from active totals.

---

## 8. Review Reports

Reports are used to analyze work logs.

Reports are read-only.

Reports do not:

* Change payment status
* Create payroll slips
* Modify worker balances

Report filters include:

* Date Range
* Workers
* Trades
* Construction Sites
* Payment Status

Report views include:

* Detailed Logs
* Summary by Worker
* Summary by Trade
* Summary by Construction Site
* Summary by Date

Reports display:

* Worker Name
* Trade
* Construction Site
* Work Date
* Start Time
* End Time
* Duration Hours
* Hourly Rate
* Total Amount
* Payment Status

Reports can be used to understand:

* Worker salary for a selected period
* Construction site labor cost
* Total labor cost
* Paid versus unpaid work
* Cost by trade

---

## 9. Export Reports

Filtered reports can be exported to Excel.

The exported file should include:

* Selected filters
* Detailed rows
* Totals
* Summary information where applicable

Excel export is used for sharing, printing, and external review.

---

## 10. Generate Payroll Slips

Payroll Slips represent payment documents for workers.

A payroll slip is generated for one worker at a time.

The user selects:

* Worker
* Date From
* Date To

The system retrieves unpaid work logs for that worker and period.

When the payroll slip is generated:

* The selected unpaid logs are included in the slip.
* Payroll slip lines are created as historical snapshots.
* Total hours are calculated.
* Total amount is calculated.
* The user enters the amount paid.
* The payroll slip status is set based on payment.

Possible payroll slip statuses:

* Paid
* Partially Paid
* Cancelled

Business rules:

* A work log cannot be included in more than one payroll slip.
* Payroll slip lines must preserve historical values.
* Payroll slips should not be deleted.
* Payroll records remain available for history and auditing.

---

## 11. Record Payroll Payments

Payroll payments track money paid to workers.

A payroll slip can be fully paid or partially paid.

Example:

Payroll Total: 1,800 USD
Amount Paid: 1,200 USD
Remaining Balance: 600 USD
Status: Partially Paid

If a payroll slip is partially paid, additional payments can be added later.

When additional payments are added:

* Total paid amount increases.
* Remaining balance decreases.
* If remaining balance becomes zero, the slip becomes Paid.

---

## 12. Track Worker Balances

Worker Balances show how much each worker has earned, how much has been paid, and how much remains unpaid.

Balance values are calculated from:

* Payroll Slips
* Payroll Payments

Worker balances are not calculated directly from raw Work Logs.

The balance page displays:

* Worker Name
* Trade
* Total Earned
* Total Paid
* Remaining Balance
* Payroll Slips Count
* Last Payment Date
* Payment Status

Payment status examples:

* Fully Paid
* Partially Paid
* Has Balance

---

## 13. Payroll Cancellation

Payroll slips cannot be deleted.

If a payroll slip was generated by mistake, it may be cancelled depending on its payment state.

If the payroll slip has no payments:

* The slip can be cancelled.
* The included work logs return to Unpaid.
* The cancelled slip remains visible in history.

If the payroll slip has payments:

* Direct cancellation is not allowed.
* The system should require an adjustment process instead.

Cancelled payroll slips are excluded from active balance totals but remain visible for auditing.

---

## 14. Backup and Restore

Because the application runs locally, data protection is important.

The user can:

* Create a database backup.
* Restore the database from a backup file.

Backups protect against:

* PC failure
* Accidental deletion
* Database corruption
* User mistakes

---

## 15. End-to-End Example

Example workflow:

1. Create a trade named Mason.
2. Create a worker named Ahmad.
3. Assign Ahmad to the Mason trade.
4. Set Ahmad's hourly rate to 10 USD.
5. Create a construction site named Site A.
6. Assign Ahmad to Site A.
7. Add 20 work logs for Ahmad in May.
8. Each log is from 08:00 to 17:00.
9. Each log equals 9 hours.
10. Each log amount is 90 USD.
11. Total May amount is 1,800 USD.
12. Generate a report for Ahmad for May.
13. Generate a payroll slip for Ahmad from unpaid May logs.
14. Enter a payment of 1,200 USD.
15. Payroll slip status becomes Partially Paid.
16. Worker balance shows 600 USD remaining.
17. Add another payment of 600 USD.
18. Payroll slip status becomes Paid.
19. Worker balance becomes fully paid.

---

## 16. Main Business Rule Summary

* Trades are selected when creating workers.
* Workers are assigned to construction sites.
* Work Logs are always created as Unpaid.
* Users cannot manually mark Work Logs as Paid.
* Payroll processing controls payment status.
* Hourly rates are snapshotted when Work Logs are created.
* Reports are read-only.
* Payroll Slips create historical snapshots.
* Work Logs cannot be paid twice.
* Worker Balances are calculated from Payroll Slips and Payments.
* Cancelled records remain visible for audit history.
* Backup and restore are required because the app stores data locally.
