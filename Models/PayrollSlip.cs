namespace Site_Workforce_Manager.Models;

public class PayrollSlip
{
    public int Id { get; set; }
    public string SlipNumber { get; set; } = string.Empty;
    public int WorkerId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public PayrollSlipStatus Status { get; set; } = PayrollSlipStatus.PartiallyPaid;
    public DateTime CreatedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Worker? Worker { get; set; }
    public ICollection<PayrollSlipLine> PayrollSlipLines { get; set; } = new List<PayrollSlipLine>();
    public ICollection<PayrollPayment> PayrollPayments { get; set; } = new List<PayrollPayment>();
}
