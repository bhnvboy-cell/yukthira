using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Dashboard
{
    [Required, StringLength(20)] public string DashboardId { get; set; } = "DB-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Category { get; set; } = "Sales";
    [StringLength(500)] public string ConfigJson { get; set; } = "{}";
    [StringLength(20)] public string Status { get; set; } = "Active";
}
