using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class StorageLocation
{
    [Required, StringLength(20)] public string Code { get; set; } = "SL-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Type { get; set; } = "General";
    public decimal Capacity { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Active";
}
