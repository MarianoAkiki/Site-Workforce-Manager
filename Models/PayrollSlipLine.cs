namespace Site_Workforce_Manager.Models;

public class PayrollSlipLine
{
    public int Id { get; set; }
    public int PayrollSlipId { get; set; }
    public int WorkLogId { get; set; }
    public string WorkerNameSnapshot { get; set; } = string.Empty;
    public string TradeNameSnapshot { get; set; } = string.Empty;
    public string ConstructionSiteNameSnapshot { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal DurationHours { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal TotalAmountSnapshot { get; set; }

    public PayrollSlip? PayrollSlip { get; set; }
    public WorkLog? WorkLog { get; set; }
}
