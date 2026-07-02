using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class WorkCenter
{
    [Required, StringLength(20)] public string Code { get; set; } = "WC-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Department { get; set; } = string.Empty;
    public decimal CapacityPerShift { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Active";
}
