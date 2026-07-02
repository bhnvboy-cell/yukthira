using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Delivery
{
    [Required, StringLength(20)] public string DeliveryNumber { get; set; } = "DN-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(20)] public string SoNumber { get; set; } = string.Empty;
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(50)] public string Status { get; set; } = "Picked";
}
