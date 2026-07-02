using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Specification
{
    [Required, StringLength(20)] public string SpecId { get; set; } = "SPEC-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(100)] public string Characteristic { get; set; } = string.Empty;
    [StringLength(50)] public string MinValue { get; set; } = string.Empty;
    [StringLength(50)] public string MaxValue { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
