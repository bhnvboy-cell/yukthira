using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class PurchaseRequisition
{
    [Required, StringLength(20)] public string PrNumber { get; set; } = "PR-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(50)] public string Requestor { get; set; } = string.Empty;
    [StringLength(100)] public string ItemName { get; set; } = string.Empty;
    [StringLength(20)] public string Quantity { get; set; } = "1";
    public decimal Amount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
