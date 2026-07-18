namespace Site_Workforce_Manager.Models;

public class ConstructionSite
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime? DeactivatedAt { get; set; }

    public ICollection<WorkerConstructionSite> WorkerConstructionSites { get; set; } = new List<WorkerConstructionSite>();
    public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
}
