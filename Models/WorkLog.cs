namespace Site_Workforce_Manager.Models;

public class WorkLog
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public int ConstructionSiteId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal DurationHours { get; set; }
    public decimal DailyRateSnapshot { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Worker? Worker { get; set; }
    public ConstructionSite? ConstructionSite { get; set; }
}
