using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Opportunity
{
    [Required, StringLength(20)] public string OppId { get; set; } = "OPP-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string OpportunityName { get; set; } = string.Empty;
    [StringLength(100)] public string Company { get; set; } = string.Empty;
    public decimal Value { get; set; }
    [StringLength(30)] public string Stage { get; set; } = "Prospecting";
    [StringLength(20)] public string Status { get; set; } = "Open";
}
