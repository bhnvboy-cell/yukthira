using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Quotation
{
    [Required, StringLength(20)] public string QuoteNumber { get; set; } = "QTE-" + Guid.NewGuid().ToString()[..4];
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Range(0, 999999.99)] public decimal Amount { get; set; }
    public DateTime ValidUntil { get; set; } = DateTime.Today.AddDays(30);
    [StringLength(20)] public string Status { get; set; } = "Draft";
}
