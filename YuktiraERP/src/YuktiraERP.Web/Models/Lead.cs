using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Lead
{
    [Required, StringLength(20)] public string LeadId { get; set; } = "LD-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Company { get; set; } = string.Empty;
    [StringLength(100)] public string Contact { get; set; } = string.Empty;
    [StringLength(50)] public string Source { get; set; } = "Website";
    public decimal Value { get; set; }
    [StringLength(20)] public string Status { get; set; } = "New";
}
