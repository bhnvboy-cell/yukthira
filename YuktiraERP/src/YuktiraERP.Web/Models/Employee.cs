using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Employee
{
    [Required, StringLength(20)] public string Code { get; set; } = "EMP-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Department { get; set; } = string.Empty;
    [StringLength(50)] public string Designation { get; set; } = string.Empty;
    [StringLength(20)] public string Mobile { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
