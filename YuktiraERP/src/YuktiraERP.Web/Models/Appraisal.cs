using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Appraisal
{
    [Required, StringLength(20)] public string AppraisalId { get; set; } = "APR-" + Guid.NewGuid().ToString()[..4];
    [StringLength(20)] public string EmployeeCode { get; set; } = string.Empty;
    [StringLength(100)] public string EmployeeName { get; set; } = string.Empty;
    [StringLength(30)] public string Period { get; set; } = "Q1 2024";
    public int Rating { get; set; } = 3;
    [StringLength(500)] public string Comments { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
