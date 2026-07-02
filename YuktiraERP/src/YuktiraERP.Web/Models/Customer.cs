using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Customer
{
    [Required, StringLength(20)] public string Code { get; set; } = "CUST-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    [StringLength(20)] public string PaymentTerms { get; set; } = "Net 30";
    [StringLength(20)] public string Phone { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
