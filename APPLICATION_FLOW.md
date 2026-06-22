# Site Workforce Manager - Application Flow

## Purpose

This document explains the current business flow of the Site Workforce Manager application from setup to daily usage, weekly work entry, reporting, weekly payments, balance review, Excel export, and backup.

---

## 1. Dashboard

The Dashboard is the application home page.

It provides a quick overview of the business status, including:

* Active Workers
* Active Construction Sites
* Total Hours This Month
* Total Labor Cost This Month
* Work Logs Count

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
* Daily Rate History
* Construction Site Assignments

A worker must be linked to a trade.

Workers can be activated or deactivated.

---

## 4. Manage Worker Daily Rates

Each worker can have daily rate history.

When a worker's daily rate changes:

* The old rate is not overwritten.
* A new rate record is created.
* The new rate has an effective date.
* Historical records remain accurate.

When a work log is created, the system uses the worker daily rate that is valid for the selected work date.

The selected rate is saved as a snapshot inside the work log.

---

## 5. Manage Construction Sites

The user creates and manages construction sites.

Construction site information includes:

* Site Name
* Location or Description
* Status

Construction sites can be activated or deactivated.

Inactive sites should not be used for new work logs.

---

## 6. Assign Workers to Construction Sites

Workers can be assigned to one or multiple construction sites.

A construction site can also have multiple assigned workers.

Business rules:

* Duplicate assignments are not allowed.
* Inactive workers cannot be assigned.
* Inactive construction sites cannot be assigned.
* Work Log site selection may be restricted to sites assigned to the selected worker.

---

## 7. Create Work Logs

Work Logs represent daily work performed by workers.

Work logs are filled from the Weekly Entry page.

Weekly Entry opens on the current Thursday-to-Wednesday week by default.

The user can move to previous weeks and edit them when needed.

For each work log, the user selects:

* Worker
* Construction Site
* Work Date
* Duration Hours

The system automatically calculates:

* Daily Rate Snapshot
* Total Amount

Formula:

Hourly value = Daily Rate Snapshot / 8

Total Amount = Duration Hours x Hourly value

Business rules:

* Worker is required.
* Construction Site is required.
* Duration hours must be greater than zero.
* A valid daily rate must exist for the worker and work date.
* If a worker has only one assigned construction site it is auto-selected on page load.
* A work log is auto-saved when both duration hours and construction site are filled, triggered when the duration field loses focus.
* Editing an existing weekly cell updates the existing work log for that worker and date.
* The full week does not need to be filled before saving.
* Weekly Entry does not use payment status.

---

## 8. Review Reports

Reports are used to analyze work logs.

Reports are read-only.

Reports do not:

* Modify worker balances
* Save payments
* Change work logs

Report filters include:

* Date Range
* Workers
* Trades
* Construction Sites

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
* Duration Hours
* Daily Rate
* Total Amount

Reports can be used to understand:

* Worker labor cost for a selected period
* Construction site labor cost
* Total labor cost
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

## 10. Weekly Payroll Payments

Payroll currently works as weekly worker payment entry, not payroll slips.

The Payroll page uses the same Thursday-to-Wednesday week structure.

The payroll table is grouped by trade and displays:

* Worker ID
* Worker Name
* Balance (for the opened week)
* Payment Amount

The balance shown for any week = all work logs with work date up to the week end minus all payments with payment date up to the week end. This means viewing week Jun 11–17 shows the exact financial position as of Jun 17, regardless of what was entered later.

For the latest completed payroll week:

* Balance is shown.
* Payment amount can be entered per worker.
* Payments auto-save per worker when the payment field loses focus.
* Payments are saved into the `WorkerPayment` table with `PaymentDate = today` and `WeekStartDate` identifying the payroll week.
* Re-entering a payment for the same worker and week updates the existing record and refreshes the payment date.

For previous payroll weeks:

* The page is read-only.
* It shows the paid amount recorded for that week.

Each worker row has a live balance button (⊙) that opens a popup showing the worker's true current balance across all time, computed on demand.

Business rules:

* Payment amount cannot be negative.
* Payment amount can exceed the worker balance (negative balance is allowed).
* Empty or zero payment removes the weekly payment entry if one exists.
* Existing weekly payment entries are updated instead of duplicated.
* Payment date is always the actual date the payment is made, not the week end date.

---

## 11. Worker Balances

Worker balances are calculated, not stored as a separate balance table.

Balance calculation:

Total Balance = Sum of WorkLog TotalAmount - Sum of WorkerPayment Amount

The system computes balances from:

* Saved work logs
* Saved worker payments

This keeps the balance safer because the source records remain the authority.

The dedicated Worker Balances page shows all active workers with:

* Total Earned (all work logs, all time)
* Total Paid (all payments, all time)
* Current Balance

The page supports filtering by worker name, trade, and an "outstanding balance only" toggle. Grand totals are shown for the filtered results.

---

## 12. Backup and Restore

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

## 13. End-to-End Example

Example workflow:

1. Create a trade named Mason.
2. Create a worker named Ahmad.
3. Assign Ahmad to the Mason trade.
4. Set Ahmad's daily rate to 80 USD.
5. Create a construction site named Site A.
6. Assign Ahmad to Site A.
7. Open Weekly Entry for the current week.
8. Select the Mason trade.
9. Enter Ahmad's hours and assigned construction site for one or more days.
10. The weekly cells auto-save as work logs.
11. Generate a report for Ahmad for the selected period.
12. Export the filtered report to Excel if needed.
13. Open Payroll for the latest completed week.
14. Enter a payment amount for Ahmad.
15. Save the weekly payment.
16. Review Ahmad's computed balance.
17. Back up the database.

---

## 14. Main Business Rule Summary

* Trades are selected when creating workers.
* Workers are assigned to construction sites.
* If a worker has only one assigned site it is auto-selected in the weekly entry grid.
* Weekly Entry opens on the current Thursday-to-Wednesday week.
* Weekly Entry supports editing current and past weeks.
* Weekly Entry auto-saves when hours and construction site are both filled, on field blur.
* Daily rates are snapshotted when Work Logs are created.
* Work log totals are calculated as Daily Rate / 8 x Duration Hours.
* Reports are read-only.
* Worker balances are calculated from work logs minus worker payments.
* Worker balances are not stored as separate balance rows.
* Payroll balance for a week = earned up to week end minus payments dated up to week end.
* Payment date is the actual date the payment is made, not the week end date.
* WeekStartDate on WorkerPayment identifies which payroll week a payment belongs to.
* Payment amounts can exceed the worker balance; negative balances are allowed.
* Weekly payments auto-save per worker on field blur.
* Previous payroll weeks are read-only.
* Worker Balances page shows all-time balance for every active worker.
* Backup and restore are required because the app stores data locally.
