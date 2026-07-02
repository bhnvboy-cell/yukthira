using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class AdminUser
{
    [Required, StringLength(20)] public string UserId { get; set; } = "USR-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string UserName { get; set; } = string.Empty;
    [StringLength(100)] public string Email { get; set; } = string.Empty;
    [StringLength(20)] public string Role { get; set; } = "READ_ONLY";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}

public class Tenant
{
    [Required, StringLength(20)] public string TenantId { get; set; } = "TNT-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Subdomain { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}

public class SystemConfig
{
    [Required, StringLength(50)] public string Key { get; set; } = string.Empty;
    [StringLength(500)] public string Value { get; set; } = string.Empty;
    [StringLength(100)] public string Description { get; set; } = string.Empty;
    [StringLength(20)] public string Module { get; set; } = "Global";
}
