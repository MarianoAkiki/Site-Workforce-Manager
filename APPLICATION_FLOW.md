# Site Workforce Manager - Application Flow

## Purpose

This document explains the current business flow of the Site Workforce Manager application from setup to daily usage, reporting, Excel export, and backup.

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

For each work log, the user selects:

* Worker
* Construction Site
* Work Date
* Duration Hours
* Notes, if needed

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

---

## 8. Review Reports

Reports are used to analyze work logs.

Reports are read-only.

Reports do not:

* Modify worker balances

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

## 10. Backup and Restore

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

## 11. End-to-End Example

Example workflow:

1. Create a trade named Mason.
2. Create a worker named Ahmad.
3. Assign Ahmad to the Mason trade.
4. Set Ahmad's daily rate to 80 USD.
5. Create a construction site named Site A.
6. Assign Ahmad to Site A.
7. Add work logs for Ahmad in May.
8. Generate a report for Ahmad for May.
9. Export the filtered report to Excel if needed.
10. Back up the database.

---

## 12. Main Business Rule Summary

* Trades are selected when creating workers.
* Workers are assigned to construction sites.
* Daily rates are snapshotted when Work Logs are created.
* Work log totals are calculated as Daily Rate / 8 x Duration Hours.
* Reports are read-only.
* Backup and restore are required because the app stores data locally.
* Payroll functionality has been removed and will be redesigned later.
