using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Vendor
{
    [Required, StringLength(20)] public string Code { get; set; } = "VEN-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(20)] public string TaxId { get; set; } = string.Empty;
    [StringLength(20)] public string PaymentTerms { get; set; } = "Net 30";
    [StringLength(20)] public string Phone { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Active";
}
