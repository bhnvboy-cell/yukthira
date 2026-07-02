namespace YuktiraERP.Core.Interfaces;

public class PayrollCalculationRequest
{
    public string EmployeeName { get; set; } = "";
    public decimal BasicPay { get; set; }
    public decimal HRA { get; set; }
    public decimal Conveyance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal Bonus { get; set; }
}

public class PayrollCalculationResult
{
    public decimal GrossPay { get; set; }
    public decimal PFEmployee { get; set; }
    public decimal PFEmployer { get; set; }
    public decimal ESIEmployee { get; set; }
    public decimal ESIEmployer { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TDSPerMonth { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal EmployerTotal { get; set; }
    public List<string> Breakdown { get; set; } = new();
}

public interface IPayrollService
{
    Task<PayrollCalculationResult> CalculatePayrollAsync(PayrollCalculationRequest request, string period = "");
}
