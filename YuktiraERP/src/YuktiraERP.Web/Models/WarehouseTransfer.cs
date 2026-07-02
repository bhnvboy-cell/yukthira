using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class WarehouseTransfer
{
    [Required, StringLength(20)] public string TransferId { get; set; } = "TRF-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(20)] public string FromBin { get; set; } = string.Empty;
    [StringLength(20)] public string ToBin { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
