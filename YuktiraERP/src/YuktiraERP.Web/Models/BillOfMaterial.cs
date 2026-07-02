using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class BillOfMaterial
{
    [Required, StringLength(20)] public string BomId { get; set; } = "BOM-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string ProductName { get; set; } = string.Empty;
    [StringLength(100)] public string ComponentName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [StringLength(10)] public string UOM { get; set; } = "EA";
    [StringLength(20)] public string Status { get; set; } = "Active";
}
