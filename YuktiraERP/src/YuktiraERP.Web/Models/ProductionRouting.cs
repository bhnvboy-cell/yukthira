using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class ProductionRouting
{
    [Required, StringLength(20)] public string RoutingId { get; set; } = "RTG-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string ProductName { get; set; } = string.Empty;
    public int OperationNo { get; set; }
    [StringLength(100)] public string WorkCenter { get; set; } = string.Empty;
    public decimal SetupTimeHrs { get; set; }
    public decimal RunTimeHrs { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Active";
}
