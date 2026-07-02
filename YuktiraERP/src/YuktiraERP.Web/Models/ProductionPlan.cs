using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class ProductionPlan
{
    [Required, StringLength(20)] public string PlanId { get; set; } = "PP-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(14);
    [StringLength(20)] public string Status { get; set; } = "Planned";
}
