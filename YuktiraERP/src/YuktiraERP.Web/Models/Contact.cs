using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Contact
{
    [Required, StringLength(20)] public string ContactId { get; set; } = "CNT-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(100)] public string Email { get; set; } = string.Empty;
    [StringLength(20)] public string Phone { get; set; } = string.Empty;
    [StringLength(100)] public string Company { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
