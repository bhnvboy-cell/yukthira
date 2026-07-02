using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Inquiry
{
    [Required, StringLength(20)] public string InquiryNumber { get; set; } = "INQ-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(200)] public string Description { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Open";
}
