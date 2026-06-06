namespace Site_Workforce_Manager.Models;

public class Worker
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Trade { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<WorkerRateHistory> RateHistory { get; set; } = new List<WorkerRateHistory>();
    public ICollection<WorkerConstructionSite> WorkerConstructionSites { get; set; } = new List<WorkerConstructionSite>();
    public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
}
