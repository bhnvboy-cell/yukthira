using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class TestResult
{
    [Required, StringLength(20)] public string ResultId { get; set; } = "TR-" + Guid.NewGuid().ToString()[..4];
    [StringLength(20)] public string SampleId { get; set; } = string.Empty;
    [StringLength(100)] public string TestName { get; set; } = string.Empty;
    [StringLength(50)] public string Result { get; set; } = string.Empty;
    [StringLength(50)] public string Specification { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
