using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class ServiceTicket
{
    [Required, StringLength(20)] public string TicketId { get; set; } = "SR-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(200)] public string Subject { get; set; } = string.Empty;
    [StringLength(20)] public string Priority { get; set; } = "Medium";
    [StringLength(20)] public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; } = DateTime.Today;
}
