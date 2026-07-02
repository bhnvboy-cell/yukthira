using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class InspectionLot
{
    [Required, StringLength(20)] public string LotNumber { get; set; } = "IQ-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(20)] public string Quantity { get; set; } = "0";
    public int Inspected { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
