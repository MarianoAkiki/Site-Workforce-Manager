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

## 2. Manage Categories (Trades)

Categories represent worker job types (e.g. Mason, Electrician, Plumber).

The user can:

* Add categories
* Edit categories
* Deactivate categories

Inactive categories are not assigned to new workers, but existing workers keep their category.

---

## 3. Manage Workers

Worker information includes:

* First Name, Last Name
* Category (Trade)
* Status (Active / Inactive)
* Daily Rate History
* Construction Site Assignments

A worker must be linked to a category. Workers can be activated or deactivated.

---

## 4. Manage Worker Daily Rates

Each worker has a daily rate history.

The rate list shows each rate with its effective date. The end date is managed internally and not shown.

When a worker's daily rate changes:

* A new rate record is added with its effective date.
* Existing work logs are not changed.
* New work logs from the effective date onward use the new rate.

When a rate value was entered incorrectly:

* Click Edit on the rate row to correct the value.
* The system automatically finds all work logs within that rate's effective period and recalculates TotalAmount and DailyRateSnapshot.

When a work log is created, the system uses the daily rate valid for the work date, saved as a snapshot inside the work log.

---

## 5. Manage Construction Sites

Construction site information includes:

* Site Name
* Location / Description
* Status (Active / Inactive)

Inactive sites cannot be used for new work logs.

---

## 6. Assign Workers to Construction Sites

Workers can be assigned to one or multiple construction sites. A site can have multiple workers.

Rules:

* Duplicate assignments are not allowed.
* Inactive workers and inactive sites cannot be assigned.

---

## 7. Create Work Logs (Weekly Entry)

Work logs represent daily work performed by workers. They are entered on the Weekly Entry page.

Weekly Entry opens on the current Thursday-to-Wednesday week. The user can navigate to previous weeks and edit them.

For each work log entry, the user fills:

* Duration Hours (numeric, positive only)
* Construction Site

The system automatically calculates:

* Daily Rate Snapshot (from rate history at the work date)
* Total Amount = DailyRateSnapshot / 8 × DurationHours

Rules:

* A work log auto-saves when both hours and site are filled, on field blur.
* If a worker has only one assigned site it is auto-selected on load.
* Editing an existing cell updates the existing work log for that worker/date.
* The full week does not need to be filled.

---

## 8. Review Reports

Reports analyze work logs and payments. Reports are read-only.

Tabs available:

* **Work Logs** — detailed log rows
* **Payments** — worker payment records
* **By Worker** — earnings summary per worker
* **By Category** — earnings summary per category
* **By Construction Site** — earnings summary per site
* **By Date** — earnings summary per date

Filters (auto-apply on change):

* Date range
* Workers (multi-select)
* Categories / Trades (multi-select)
* Construction Sites (multi-select)

Stats shown: Total Hours, Total Earned, Total Paid.

All tabs support pagination (25/50/100 rows per page).

---

## 9. Export Reports

The currently active report tab can be exported to Excel.

The exported file includes the rows visible in that tab (full filtered set, not just the current page).

---

## 10. Payroll (Weekly Payments)

The Payroll page uses Thursday-to-Wednesday weeks.

The table is grouped by category and shows per worker:

* Worker ID and Name
* Balance for the opened week
* Payment Amount (editable for the latest week only)

**Balance for a week** = all work logs with WorkDate ≤ WeekEnd minus all payments with PaymentDate ≤ WeekEnd.

For the latest completed payroll week:

* Balance is shown.
* Payment amount can be entered; auto-saves on field blur.
* A payment is saved with PaymentDate = today and WeekStartDate identifying the payroll week.
* Re-entering a payment for the same worker and week updates the existing record.

For previous weeks:

* The page is read-only and shows the paid amount recorded for that week.

Each worker row has a live balance button that opens a popup showing the all-time current balance, computed on demand.

Negative values are displayed as parentheses, e.g. (150.00).
Category totals show blank when no payment has been entered yet.

Rules:

* Payment amount must be positive (negative input is blocked at the field level).
* Payment can exceed balance; a negative balance is allowed.
* Empty or zero payment removes the weekly payment entry.
* Payment date is the actual date of payment, not the week end date.

---

## 11. Weekly Report

The Weekly Report page shows a per-worker financial summary for a selected week, filtered by category.

The category filter is mandatory — the report is always scoped to one category.

Columns per worker:

* ID, Name
* Hours logged for each of the 7 days
* Total Hours, Number of Days
* Daily Rate
* Balance Before This Week
* This Week's Earnings
* Total Earned up to Week End
* Total Paid up to Week End
* Balance to Week End (still owed)

A totals row is shown at the bottom. The worker count excludes the totals row.

Negative values display as parentheses.

The report can be printed in Arabic (RTL, A4 landscape) with a signature column (التوقيع) and the category name in the title.

---

## 12. Worker Balances

The Worker Balances page shows the all-time financial position for every active worker.

Per worker:

* Total Earned (sum of all work log amounts, all time)
* Total Paid (sum of all payments, all time)
* Current Balance (Earned minus Paid)

Filters: worker ID, worker name, category, outstanding balance only toggle.

Grand totals are shown for the filtered result set.

Supports pagination (25/50/100 rows).

---

## 13. Maintenance

The Maintenance page allows backing up the SQLite database to a file chosen by the user.

Backups protect against PC failure, accidental deletion, or database corruption.

---

## 14. End-to-End Example

1. Create a category named Mason.
2. Create a worker named Ahmad, assign to Mason category.
3. Set Ahmad's daily rate to 80.
4. Create a construction site named Site A.
5. Assign Ahmad to Site A.
6. Open Weekly Entry for the current week.
7. Select the Mason category tab.
8. Enter Ahmad's hours for one or more days (auto-saves on blur).
9. Open Reports → filter by worker Ahmad → review earnings.
10. Export the filtered report to Excel if needed.
11. Open Payroll for the latest completed week.
12. Enter a payment amount for Ahmad (auto-saves on blur).
13. Open Weekly Report → select Mason category → review Ahmad's balance summary.
14. Open Worker Balances to see Ahmad's all-time balance.
15. Back up the database from the Maintenance page.

---

## 15. Business Rule Summary

* Categories are selected when creating workers.
* Workers are assigned to construction sites.
* If a worker has only one assigned site it is auto-selected in the weekly entry grid.
* Weekly Entry opens on the current Thursday-to-Wednesday week and supports past weeks.
* Work logs auto-save when hours and site are both filled, on blur.
* Daily rates are snapshotted at work log creation; correcting a rate bulk-recalculates affected logs.
* Work log total = Daily Rate / 8 × Duration Hours.
* Reports are read-only; filters auto-apply on change.
* Excel export targets the currently active report tab.
* Payroll balance for a week = earned up to week end minus payments dated up to week end.
* Payment date is the actual payment date, not the week end date.
* WeekStartDate on WorkerPayment identifies the payroll week.
* Payments can exceed balance; negative balances are allowed.
* Previous payroll weeks are read-only.
* Worker Balances page shows all-time balance (all logs minus all payments).
* Weekly Report always requires a category filter; prints in Arabic RTL.
* Numeric inputs (daily rate, hours, payment) block negative values and non-numeric characters.
* All list views support pagination (25/50/100 rows, default 25).
* Backup only — no restore in the current version.
