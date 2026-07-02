using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class InspectionPlan
{
    [Required, StringLength(20)] public string PlanId { get; set; } = "IP-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(100)] public string Characteristic { get; set; } = string.Empty;
    [StringLength(20)] public string Method { get; set; } = string.Empty;
    [StringLength(20)] public string Frequency { get; set; } = "Each Lot";
    [StringLength(20)] public string Status { get; set; } = "Active";
}
