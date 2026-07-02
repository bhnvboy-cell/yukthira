using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class StockItem
{
    [Required, StringLength(20)] public string Bin { get; set; } = string.Empty;
    [Required, StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(20)] public string Lot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [StringLength(10)] public string UOM { get; set; } = "EA";
    public decimal Value { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
}
