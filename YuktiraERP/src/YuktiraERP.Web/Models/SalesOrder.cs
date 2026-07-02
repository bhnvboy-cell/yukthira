using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class SalesOrder
{
    [Required, StringLength(20)] public string OrderNumber { get; set; } = "SO-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public int ItemCount { get; set; }
    public decimal Amount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
