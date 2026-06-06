namespace Site_Workforce_Manager.Models;

public class WorkerConstructionSite
{
    public int WorkerId { get; set; }
    public int ConstructionSiteId { get; set; }
    public DateTime AssignedDate { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public Worker? Worker { get; set; }
    public ConstructionSite? ConstructionSite { get; set; }
}
