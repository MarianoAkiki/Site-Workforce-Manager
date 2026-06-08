namespace Site_Workforce_Manager.Models;

public class WorkLog
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public int ConstructionSiteId { get; set; }
    public DateTime WorkDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal DurationHours { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Worker? Worker { get; set; }
    public ConstructionSite? ConstructionSite { get; set; }
    public ICollection<PayrollSlipLine> PayrollSlipLines { get; set; } = new List<PayrollSlipLine>();
}
