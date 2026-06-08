using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class PayrollViewModel : ObservableObject
{
    private bool isUpdatingFilters;

    public PayrollViewModel()
    {
        DateFrom = DateTime.Today.AddDays(-14);
        DateTo = DateTime.Today;
        PaymentDate = DateTime.Today;
        LoadWorkerOptions();
        LoadPayrollData();
    }

    public ObservableCollection<LookupOption> WorkerOptions { get; } = new();
    public ObservableCollection<PayrollWorkLogRow> AvailableWorkLogs { get; } = new();
    public ObservableCollection<PayrollSlipHistoryRow> PayrollSlips { get; } = new();
    public ObservableCollection<PayrollSlipLineRow> SelectedSlipLines { get; } = new();
    public ObservableCollection<PayrollPaymentRow> SelectedSlipPayments { get; } = new();

    [ObservableProperty]
    private LookupOption? selectedWorkerOption;

    [ObservableProperty]
    private DateTime? dateFrom;

    [ObservableProperty]
    private DateTime? dateTo;

    [ObservableProperty]
    private DateTime paymentDate;

    [ObservableProperty]
    private decimal amountPaid;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private decimal availableTotalHours;

    [ObservableProperty]
    private decimal availableTotalAmount;

    [ObservableProperty]
    private string availableLogsMessage = "Select a worker and date range to load unpaid work logs.";

    [ObservableProperty]
    private PayrollSlipHistoryRow? selectedPayrollSlip;

    [ObservableProperty]
    private decimal selectedSlipTotalHours;

    [ObservableProperty]
    private decimal selectedSlipTotalAmount;

    [ObservableProperty]
    private decimal selectedSlipAmountPaid;

    [ObservableProperty]
    private decimal selectedSlipRemainingBalance;

    [ObservableProperty]
    private string selectedSlipStatusText = string.Empty;

    [ObservableProperty]
    private DateTime additionalPaymentDate = DateTime.Today;

    [ObservableProperty]
    private decimal additionalPaymentAmount;

    [ObservableProperty]
    private string additionalPaymentNotes = string.Empty;

    public bool CanAddPaymentToSelectedSlip =>
        SelectedPayrollSlip is not null &&
        SelectedSlipRemainingBalance > 0m &&
        !string.Equals(SelectedSlipStatusText, PayrollSlipStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase);

    public bool CanCancelSelectedSlip =>
        SelectedPayrollSlip is not null &&
        !string.Equals(SelectedSlipStatusText, PayrollSlipStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase);

    partial void OnSelectedWorkerOptionChanged(LookupOption? value)
    {
        if (!isUpdatingFilters)
        {
            LoadPayrollData();
        }
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        if (!isUpdatingFilters)
        {
            LoadPayrollData();
        }
    }

    partial void OnDateToChanged(DateTime? value)
    {
        if (!isUpdatingFilters)
        {
            LoadPayrollData();
        }
    }

    partial void OnSelectedPayrollSlipChanged(PayrollSlipHistoryRow? value)
    {
        LoadSelectedSlipDetails();
        OnPropertyChanged(nameof(CanAddPaymentToSelectedSlip));
        OnPropertyChanged(nameof(CanCancelSelectedSlip));
    }

    public void LoadPayrollPage()
    {
        LoadWorkerOptions();
        LoadPayrollData();
    }

    [RelayCommand]
    private void LoadPayrollData()
    {
        LoadAvailableWorkLogs();
        LoadPayrollHistory();
    }

    [RelayCommand]
    private void GeneratePayrollSlip()
    {
        if (SelectedWorkerOption?.Id is not int workerId)
        {
            MessageBox.Show("Please select one worker before generating a payroll slip.");
            return;
        }

        if (!DateFrom.HasValue || !DateTo.HasValue)
        {
            MessageBox.Show("Please select a valid payroll date range.");
            return;
        }

        if (DateTo.Value.Date < DateFrom.Value.Date)
        {
            MessageBox.Show("Date To must be on or after Date From.");
            return;
        }

        if (AvailableWorkLogs.Count == 0)
        {
            MessageBox.Show("There are no unpaid work logs for the selected worker and period.");
            return;
        }

        if (AmountPaid < 0)
        {
            MessageBox.Show("Amount paid cannot be negative.");
            return;
        }

        if (AmountPaid > AvailableTotalAmount)
        {
            MessageBox.Show("Amount paid cannot be greater than the total amount.");
            return;
        }

        using var context = new AppDbContext();
        using var transaction = context.Database.BeginTransaction();

        var workLogs = context.WorkLogs
            .Include(workLog => workLog.Worker)
            .ThenInclude(worker => worker!.Trade)
            .Include(workLog => workLog.ConstructionSite)
            .Where(workLog => workLog.WorkerId == workerId &&
                              workLog.PaymentStatus == PaymentStatus.Unpaid &&
                              workLog.WorkDate >= DateFrom.Value.Date &&
                              workLog.WorkDate <= DateTo.Value.Date)
            .OrderBy(workLog => workLog.WorkDate)
            .ThenBy(workLog => workLog.Id)
            .ToList();

        var workLogIds = workLogs.Select(workLog => workLog.Id).ToList();

        if (workLogs.Count == 0)
        {
            MessageBox.Show("The selected worker no longer has unpaid work logs for that period.");
            return;
        }

        var alreadyUsedLogIds = context.PayrollSlipLines
            .Where(line => workLogIds.Contains(line.WorkLogId))
            .Select(line => line.WorkLogId)
            .ToHashSet();

        if (alreadyUsedLogIds.Count > 0)
        {
            MessageBox.Show("One or more work logs are already included in another payroll slip.");
            return;
        }

        var totalHours = Math.Round(workLogs.Sum(workLog => workLog.DurationHours), 2);
        var totalAmount = Math.Round(workLogs.Sum(workLog => workLog.TotalAmount), 2);
        var amountPaid = Math.Round(AmountPaid, 2);
        var remainingBalance = Math.Round(totalAmount - amountPaid, 2);
        var status = remainingBalance == 0m
            ? PayrollSlipStatus.Paid
            : PayrollSlipStatus.PartiallyPaid;
        var createdAt = DateTime.Now;

        var payrollSlip = new PayrollSlip
        {
            WorkerId = workerId,
            DateFrom = DateFrom.Value.Date,
            DateTo = DateTo.Value.Date,
            TotalHours = totalHours,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            RemainingBalance = remainingBalance,
            Status = status,
            CreatedAt = createdAt,
            Notes = Notes.Trim()
        };

        context.PayrollSlips.Add(payrollSlip);
        context.SaveChanges();

        payrollSlip.SlipNumber = $"PS-{createdAt:yyyyMMdd}-{payrollSlip.Id:D4}";

        foreach (var workLog in workLogs)
        {
            context.PayrollSlipLines.Add(new PayrollSlipLine
            {
                PayrollSlipId = payrollSlip.Id,
                WorkLogId = workLog.Id,
                WorkerNameSnapshot = $"{workLog.Worker?.FirstName} {workLog.Worker?.LastName}".Trim(),
                TradeNameSnapshot = workLog.Worker?.Trade?.Name ?? "Unassigned",
                ConstructionSiteNameSnapshot = workLog.ConstructionSite?.Name ?? string.Empty,
                WorkDate = workLog.WorkDate,
                StartTime = workLog.StartTime,
                EndTime = workLog.EndTime,
                DurationHours = workLog.DurationHours,
                HourlyRateSnapshot = workLog.HourlyRateSnapshot,
                TotalAmountSnapshot = workLog.TotalAmount
            });

            workLog.PaymentStatus = PaymentStatus.Paid;
            workLog.UpdatedAt = createdAt;
        }

        if (amountPaid > 0)
        {
            context.PayrollPayments.Add(new PayrollPayment
            {
                PayrollSlipId = payrollSlip.Id,
                PaymentDate = PaymentDate.Date,
                Amount = amountPaid,
                Notes = "Initial payroll payment"
            });
        }

        context.SaveChanges();
        transaction.Commit();

        MessageBox.Show("Payroll slip generated successfully.");

        ClearEntryFields();
        LoadPayrollData();
        SelectedPayrollSlip = PayrollSlips.FirstOrDefault(item => item.Id == payrollSlip.Id);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        isUpdatingFilters = true;
        SelectedWorkerOption = WorkerOptions.FirstOrDefault();
        DateFrom = DateTime.Today.AddDays(-14);
        DateTo = DateTime.Today;
        isUpdatingFilters = false;

        LoadPayrollData();
    }

    [RelayCommand]
    private void AddPaymentToSelectedSlip()
    {
        if (SelectedPayrollSlip is null)
        {
            MessageBox.Show("Please select a payroll slip first.");
            return;
        }

        if (AdditionalPaymentAmount <= 0)
        {
            MessageBox.Show("Please enter a payment amount greater than zero.");
            return;
        }

        using var context = new AppDbContext();

        var slip = context.PayrollSlips
            .Include(item => item.PayrollPayments)
            .FirstOrDefault(item => item.Id == SelectedPayrollSlip.Id);

        if (slip is null)
        {
            MessageBox.Show("The selected payroll slip could not be found.");
            return;
        }

        if (slip.Status == PayrollSlipStatus.Cancelled)
        {
            MessageBox.Show("Cancelled payroll slips cannot receive payments.");
            return;
        }

        if (slip.RemainingBalance <= 0)
        {
            MessageBox.Show("This payroll slip is already fully paid.");
            return;
        }

        var paymentAmount = Math.Round(AdditionalPaymentAmount, 2);

        if (paymentAmount > slip.RemainingBalance)
        {
            MessageBox.Show("Payment amount cannot be greater than the remaining balance.");
            return;
        }

        context.PayrollPayments.Add(new PayrollPayment
        {
            PayrollSlipId = slip.Id,
            PaymentDate = AdditionalPaymentDate.Date,
            Amount = paymentAmount,
            Notes = AdditionalPaymentNotes.Trim()
        });

        slip.AmountPaid = Math.Round(slip.AmountPaid + paymentAmount, 2);
        slip.RemainingBalance = Math.Round(slip.TotalAmount - slip.AmountPaid, 2);
        slip.Status = slip.RemainingBalance == 0m
            ? PayrollSlipStatus.Paid
            : PayrollSlipStatus.PartiallyPaid;

        context.SaveChanges();

        MessageBox.Show("Payment added successfully.");

        ClearAdditionalPaymentFields();
        LoadPayrollData();
        SelectedPayrollSlip = PayrollSlips.FirstOrDefault(item => item.Id == slip.Id);
    }

    [RelayCommand]
    private void CancelSelectedSlip()
    {
        if (SelectedPayrollSlip is null)
        {
            MessageBox.Show("Please select a payroll slip first.");
            return;
        }

        var confirmation = MessageBox.Show(
            "Are you sure you want to cancel this payroll slip?",
            "Cancel Payroll Slip",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        using var context = new AppDbContext();
        using var transaction = context.Database.BeginTransaction();

        var slip = context.PayrollSlips
            .Include(item => item.PayrollPayments)
            .Include(item => item.PayrollSlipLines)
            .FirstOrDefault(item => item.Id == SelectedPayrollSlip.Id);

        if (slip is null)
        {
            MessageBox.Show("The selected payroll slip could not be found.");
            return;
        }

        if (slip.Status == PayrollSlipStatus.Cancelled)
        {
            MessageBox.Show("This payroll slip is already cancelled.");
            return;
        }

        if (slip.PayrollPayments.Any())
        {
            MessageBox.Show("This payroll slip has payments and cannot be cancelled directly. Create an adjustment instead.");
            return;
        }

        var workLogIds = slip.PayrollSlipLines
            .Select(line => line.WorkLogId)
            .ToList();

        var workLogs = context.WorkLogs
            .Where(workLog => workLogIds.Contains(workLog.Id))
            .ToList();

        foreach (var workLog in workLogs)
        {
            workLog.PaymentStatus = PaymentStatus.Unpaid;
            workLog.UpdatedAt = DateTime.Now;
        }

        slip.Status = PayrollSlipStatus.Cancelled;
        slip.AmountPaid = 0m;
        slip.RemainingBalance = slip.TotalAmount;

        context.SaveChanges();
        transaction.Commit();

        MessageBox.Show("Payroll slip cancelled successfully.");

        LoadPayrollData();
        SelectedPayrollSlip = PayrollSlips.FirstOrDefault(item => item.Id == slip.Id);
    }

    private void LoadWorkerOptions()
    {
        using var context = new AppDbContext();

        var existingSelection = SelectedWorkerOption?.Id;

        var workerOptions = context.Workers
            .AsNoTracking()
            .Where(worker => worker.Status == EntityStatus.Active)
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Select(worker => new LookupOption
            {
                Id = worker.Id,
                Name = $"{worker.FirstName} {worker.LastName}"
            })
            .ToList();

        WorkerOptions.Clear();
        WorkerOptions.Add(new LookupOption { Id = null, Name = "Select Worker" });

        foreach (var worker in workerOptions)
        {
            WorkerOptions.Add(worker);
        }

        isUpdatingFilters = true;
        SelectedWorkerOption = WorkerOptions.FirstOrDefault(option => option.Id == existingSelection) ?? WorkerOptions.FirstOrDefault();
        isUpdatingFilters = false;
    }

    private void LoadAvailableWorkLogs()
    {
        AvailableWorkLogs.Clear();

        if (SelectedWorkerOption?.Id is not int workerId)
        {
            AvailableTotalHours = 0m;
            AvailableTotalAmount = 0m;
            AvailableLogsMessage = "Select one worker to load unpaid work logs.";
            return;
        }

        if (!DateFrom.HasValue || !DateTo.HasValue)
        {
            AvailableTotalHours = 0m;
            AvailableTotalAmount = 0m;
            AvailableLogsMessage = "Select a valid date range to load unpaid work logs.";
            return;
        }

        var startDate = DateFrom.Value.Date;
        var endDate = DateTo.Value.Date;

        if (endDate < startDate)
        {
            AvailableTotalHours = 0m;
            AvailableTotalAmount = 0m;
            AvailableLogsMessage = "Date To must be on or after Date From.";
            return;
        }

        using var context = new AppDbContext();

        var usedWorkLogIds = context.PayrollSlipLines
            .AsNoTracking()
            .Select(line => line.WorkLogId)
            .ToHashSet();

        var workLogs = context.WorkLogs
            .AsNoTracking()
            .Include(workLog => workLog.Worker)
            .ThenInclude(worker => worker!.Trade)
            .Include(workLog => workLog.ConstructionSite)
            .Where(workLog => workLog.WorkerId == workerId &&
                              workLog.PaymentStatus == PaymentStatus.Unpaid &&
                              workLog.WorkDate >= startDate &&
                              workLog.WorkDate <= endDate &&
                              !usedWorkLogIds.Contains(workLog.Id))
            .OrderBy(workLog => workLog.WorkDate)
            .ThenBy(workLog => workLog.Id)
            .ToList();

        foreach (var workLog in workLogs)
        {
            AvailableWorkLogs.Add(new PayrollWorkLogRow
            {
                Id = workLog.Id,
                WorkerName = $"{workLog.Worker?.FirstName} {workLog.Worker?.LastName}".Trim(),
                TradeName = workLog.Worker?.Trade?.Name ?? "Unassigned",
                ConstructionSiteName = workLog.ConstructionSite?.Name ?? string.Empty,
                WorkDate = workLog.WorkDate,
                StartTime = workLog.StartTime,
                EndTime = workLog.EndTime,
                DurationHours = workLog.DurationHours,
                HourlyRate = workLog.HourlyRateSnapshot,
                TotalAmount = workLog.TotalAmount,
                Notes = workLog.Notes
            });
        }

        AvailableTotalHours = Math.Round(workLogs.Sum(workLog => workLog.DurationHours), 2);
        AvailableTotalAmount = Math.Round(workLogs.Sum(workLog => workLog.TotalAmount), 2);
        AvailableLogsMessage = workLogs.Count == 0
            ? "No unpaid work logs were found for this worker and date range."
            : string.Empty;
    }

    private void LoadPayrollHistory()
    {
        using var context = new AppDbContext();

        var query = context.PayrollSlips
            .AsNoTracking()
            .Include(slip => slip.Worker)
            .AsQueryable();

        if (SelectedWorkerOption?.Id is int workerId)
        {
            query = query.Where(slip => slip.WorkerId == workerId);
        }

        if (DateFrom.HasValue)
        {
            var startDate = DateFrom.Value.Date;
            query = query.Where(slip => slip.DateTo >= startDate);
        }

        if (DateTo.HasValue)
        {
            var endDate = DateTo.Value.Date;
            query = query.Where(slip => slip.DateFrom <= endDate);
        }

        var selectedSlipId = SelectedPayrollSlip?.Id;

        var slips = query
            .OrderByDescending(slip => slip.CreatedAt)
            .ThenByDescending(slip => slip.Id)
            .ToList();

        PayrollSlips.Clear();

        foreach (var slip in slips)
        {
            PayrollSlips.Add(new PayrollSlipHistoryRow
            {
                Id = slip.Id,
                SlipNumber = slip.SlipNumber,
                WorkerName = $"{slip.Worker?.FirstName} {slip.Worker?.LastName}".Trim(),
                DateFrom = slip.DateFrom,
                DateTo = slip.DateTo,
                TotalHours = slip.TotalHours,
                TotalAmount = slip.TotalAmount,
                AmountPaid = slip.AmountPaid,
                RemainingBalance = slip.RemainingBalance,
                Status = slip.Status,
                CreatedAt = slip.CreatedAt,
                Notes = slip.Notes
            });
        }

        SelectedPayrollSlip = PayrollSlips.FirstOrDefault(item => item.Id == selectedSlipId)
                              ?? PayrollSlips.FirstOrDefault();
    }

    private void LoadSelectedSlipDetails()
    {
        SelectedSlipLines.Clear();
        SelectedSlipPayments.Clear();

        if (SelectedPayrollSlip is null)
        {
            SelectedSlipTotalHours = 0m;
            SelectedSlipTotalAmount = 0m;
            SelectedSlipAmountPaid = 0m;
            SelectedSlipRemainingBalance = 0m;
            SelectedSlipStatusText = string.Empty;
            ClearAdditionalPaymentFields();
            OnPropertyChanged(nameof(CanAddPaymentToSelectedSlip));
            OnPropertyChanged(nameof(CanCancelSelectedSlip));
            return;
        }

        using var context = new AppDbContext();

        var slip = context.PayrollSlips
            .AsNoTracking()
            .Include(item => item.PayrollSlipLines)
            .Include(item => item.PayrollPayments)
            .FirstOrDefault(item => item.Id == SelectedPayrollSlip.Id);

        if (slip is null)
        {
            return;
        }

        foreach (var line in slip.PayrollSlipLines
                     .OrderBy(line => line.WorkDate)
                     .ThenBy(line => line.Id))
        {
            SelectedSlipLines.Add(new PayrollSlipLineRow
            {
                WorkLogId = line.WorkLogId,
                WorkerName = line.WorkerNameSnapshot,
                TradeName = line.TradeNameSnapshot,
                ConstructionSiteName = line.ConstructionSiteNameSnapshot,
                WorkDate = line.WorkDate,
                StartTime = line.StartTime,
                EndTime = line.EndTime,
                DurationHours = line.DurationHours,
                HourlyRate = line.HourlyRateSnapshot,
                TotalAmount = line.TotalAmountSnapshot
            });
        }

        foreach (var payment in slip.PayrollPayments
                     .OrderByDescending(payment => payment.PaymentDate)
                     .ThenByDescending(payment => payment.Id))
        {
            SelectedSlipPayments.Add(new PayrollPaymentRow
            {
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                Notes = payment.Notes
            });
        }

        SelectedSlipTotalHours = slip.TotalHours;
        SelectedSlipTotalAmount = slip.TotalAmount;
        SelectedSlipAmountPaid = slip.AmountPaid;
        SelectedSlipRemainingBalance = slip.RemainingBalance;
        SelectedSlipStatusText = slip.Status.ToString();
        AdditionalPaymentDate = DateTime.Today;
        AdditionalPaymentAmount = 0m;
        AdditionalPaymentNotes = string.Empty;
        OnPropertyChanged(nameof(CanAddPaymentToSelectedSlip));
        OnPropertyChanged(nameof(CanCancelSelectedSlip));
    }

    private void ClearEntryFields()
    {
        AmountPaid = 0m;
        PaymentDate = DateTime.Today;
        Notes = string.Empty;
    }

    private void ClearAdditionalPaymentFields()
    {
        AdditionalPaymentDate = DateTime.Today;
        AdditionalPaymentAmount = 0m;
        AdditionalPaymentNotes = string.Empty;
    }

    public class PayrollWorkLogRow
    {
        public int Id { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string ConstructionSiteName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class PayrollSlipHistoryRow
    {
        public int Id { get; set; }
        public string SlipNumber { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public PayrollSlipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class PayrollSlipLineRow
    {
        public int WorkLogId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string ConstructionSiteName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class PayrollPaymentRow
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
