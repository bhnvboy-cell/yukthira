using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class LeaveRequest
{
    [Required, StringLength(20)] public string LeaveId { get; set; } = "LV-" + Guid.NewGuid().ToString()[..4];
    [StringLength(100)] public string EmployeeName { get; set; } = string.Empty;
    [StringLength(30)] public string LeaveType { get; set; } = "Annual";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);
    [StringLength(20)] public string Status { get; set; } = "Pending";
}
