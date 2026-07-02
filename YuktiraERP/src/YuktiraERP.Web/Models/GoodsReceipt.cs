using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class GoodsReceipt
{
    [Required, StringLength(20)] public string GrnNumber { get; set; } = "GRN-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(20)] public string PoNumber { get; set; } = string.Empty;
    [StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(20)] public string QtyReceived { get; set; } = "0";
    [StringLength(20)] public string QtyAccepted { get; set; } = "0";
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
