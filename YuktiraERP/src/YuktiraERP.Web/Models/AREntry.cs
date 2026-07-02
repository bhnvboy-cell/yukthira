using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class AREntry
{
    [Required, StringLength(20)] public string DocumentNumber { get; set; } = "AR-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ReceivedAmount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Open";
}
