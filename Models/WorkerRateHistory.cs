namespace Site_Workforce_Manager.Models;

public class WorkerRateHistory
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public decimal DailyRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Worker? Worker { get; set; }
}
