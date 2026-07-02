using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class UsageDecision
{
    [Required, StringLength(20)] public string DecisionId { get; set; } = "UD-" + Guid.NewGuid().ToString()[..4];
    [StringLength(20)] public string LotNumber { get; set; } = string.Empty;
    [StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(200)] public string Decision { get; set; } = "Accept";
    [StringLength(200)] public string Notes { get; set; } = string.Empty;
    public DateTime DecisionDate { get; set; } = DateTime.Today;
}
