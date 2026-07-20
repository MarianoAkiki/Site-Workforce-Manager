namespace Site_Workforce_Manager.Helpers;

public class WorkerListItem
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public int? TradeId { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public decimal CurrentDailyRate { get; set; }
    public int AssignedSiteCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime StartedAt { get; set; }
}
