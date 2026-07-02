using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class PurchaseOrder
{
    [Required, StringLength(20)] public string PoNumber { get; set; } = "PO-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string VendorName { get; set; } = string.Empty;
    [StringLength(100)] public string ItemName { get; set; } = string.Empty;
    [StringLength(20)] public string Quantity { get; set; } = "1";
    public decimal Amount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
