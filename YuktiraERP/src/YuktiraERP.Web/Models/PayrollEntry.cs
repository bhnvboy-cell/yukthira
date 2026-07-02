using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class PayrollEntry
{
    [Required, StringLength(20)] public string PayrollId { get; set; } = "PRL-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string EmployeeName { get; set; } = string.Empty;
    [StringLength(30)] public string Period { get; set; } = "January 2024";
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Draft";
}
