using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class APEntry
{
    [Required, StringLength(20)] public string DocumentNumber { get; set; } = "AP-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string VendorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Open";
}
