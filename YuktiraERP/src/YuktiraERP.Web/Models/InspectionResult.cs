using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class InspectionResult
{
    [Required, StringLength(20)] public string ResultId { get; set; } = "IR-" + Guid.NewGuid().ToString()[..4];
    [StringLength(20)] public string LotNumber { get; set; } = string.Empty;
    [StringLength(100)] public string Characteristic { get; set; } = string.Empty;
    [StringLength(50)] public string Result { get; set; } = string.Empty;
    [StringLength(50)] public string Specification { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Passed";
}
