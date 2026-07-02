using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Attendance
{
    [Required, StringLength(20)] public string AttendanceId { get; set; } = "ATT-" + Guid.NewGuid().ToString()[..4];
    [StringLength(20)] public string EmployeeCode { get; set; } = string.Empty;
    [StringLength(100)] public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    [StringLength(20)] public string Status { get; set; } = "Present";
}
