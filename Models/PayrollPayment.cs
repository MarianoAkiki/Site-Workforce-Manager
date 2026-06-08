namespace Site_Workforce_Manager.Models;

public class PayrollPayment
{
    public int Id { get; set; }
    public int PayrollSlipId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;

    public PayrollSlip? PayrollSlip { get; set; }
}
