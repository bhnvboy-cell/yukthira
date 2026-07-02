using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class BIReport
{
    [Required, StringLength(20)] public string ReportId { get; set; } = "RPT-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string ReportName { get; set; } = string.Empty;
    [StringLength(50)] public string Category { get; set; } = string.Empty;
    [StringLength(10)] public string Format { get; set; } = "PDF";
    public DateTime LastRun { get; set; } = DateTime.Today;
    [StringLength(50)] public string CreatedBy { get; set; } = "Admin";
}
