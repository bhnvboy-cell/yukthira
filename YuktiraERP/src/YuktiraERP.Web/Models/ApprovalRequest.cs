using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class ApprovalRequest
{
    [Required, StringLength(20)] public string RequestId { get; set; } = "APR-" + Guid.NewGuid().ToString()[..4];
    [StringLength(30)] public string Type { get; set; } = string.Empty;
    [StringLength(200)] public string Subject { get; set; } = string.Empty;
    [StringLength(100)] public string Requestor { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; } = DateTime.Today;
    public decimal? Amount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
