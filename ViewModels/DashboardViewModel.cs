using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private int activeWorkers;

    [ObservableProperty]
    private int activeConstructionSites;

    [ObservableProperty]
    private decimal totalHoursThisMonth;

    [ObservableProperty]
    private decimal totalLaborCostThisMonth;

    [ObservableProperty]
    private decimal totalOutstandingBalance;

    [ObservableProperty]
    private int unpaidWorkLogsCount;

    [ObservableProperty]
    private string monthLabel = string.Empty;

    public DashboardViewModel()
    {
        LoadDashboard();
    }

    public void LoadDashboard()
    {
        using var context = new AppDbContext();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var monthlyLogs = context.WorkLogs
            .AsNoTracking()
            .Where(workLog => workLog.WorkDate >= monthStart &&
                              workLog.WorkDate <= monthEnd &&
                              workLog.PaymentStatus != PaymentStatus.Cancelled)
            .ToList();

        var activePayrollSlips = context.PayrollSlips
            .AsNoTracking()
            .Where(slip => slip.Status != PayrollSlipStatus.Cancelled)
            .ToList();

        ActiveWorkers = context.Workers
            .AsNoTracking()
            .Count(worker => worker.Status == EntityStatus.Active);

        ActiveConstructionSites = context.ConstructionSites
            .AsNoTracking()
            .Count(site => site.Status == EntityStatus.Active);

        TotalHoursThisMonth = Math.Round(monthlyLogs.Sum(workLog => workLog.DurationHours), 2);
        TotalLaborCostThisMonth = Math.Round(monthlyLogs.Sum(workLog => workLog.TotalAmount), 2);
        TotalOutstandingBalance = Math.Round(activePayrollSlips.Sum(slip => slip.RemainingBalance), 2);
        UnpaidWorkLogsCount = context.WorkLogs
            .AsNoTracking()
            .Count(workLog => workLog.PaymentStatus == PaymentStatus.Unpaid);
        MonthLabel = monthStart.ToString("MMMM yyyy");
    }
}
