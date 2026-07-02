using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class FixedAsset
{
    [Required, StringLength(20)] public string AssetCode { get; set; } = "FA-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string AssetName { get; set; } = string.Empty;
    [StringLength(50)] public string Category { get; set; } = "Equipment";
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    [Range(0, 9999999.99)] public decimal Cost { get; set; }
    [Range(0, 9999999.99)] public decimal SalvageValue { get; set; }
    public int UsefulLifeYears { get; set; } = 5;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
