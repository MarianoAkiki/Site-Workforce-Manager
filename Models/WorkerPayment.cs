namespace Site_Workforce_Manager.Models;

public class WorkerPayment
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Worker? Worker { get; set; }
}
