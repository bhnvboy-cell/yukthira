using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class BillingDocument
{
    [Required, StringLength(20)] public string DocumentNumber { get; set; } = "INV-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(20)] public string SoNumber { get; set; } = string.Empty;
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Range(0, 999999.99)] public decimal Amount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Unpaid";
}
