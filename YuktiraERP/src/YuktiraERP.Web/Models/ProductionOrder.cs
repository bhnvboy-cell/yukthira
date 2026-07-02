using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class ProductionOrder
{
    [Required, StringLength(20)] public string OrderNumber { get; set; } = "MO-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string ProductName { get; set; } = string.Empty;
    [Range(1, 999999)] public decimal Quantity { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    [StringLength(20)] public string Status { get; set; } = "Planned";
}
