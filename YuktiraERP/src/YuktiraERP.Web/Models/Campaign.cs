using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Campaign
{
    [Required, StringLength(20)] public string CampaignId { get; set; } = "CMP-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Type { get; set; } = "Email";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    [Range(0, 999999.99)] public decimal Budget { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Draft";
}
