using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class InvoiceVerification
{
    [Required, StringLength(20)] public string InvoiceNumber { get; set; } = "INV-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(20)] public string PoNumber { get; set; } = string.Empty;
    [StringLength(100)] public string VendorName { get; set; } = string.Empty;
    [Range(0, 999999.99)] public decimal Amount { get; set; }
    [Range(0, 999999.99)] public decimal MatchedAmount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
