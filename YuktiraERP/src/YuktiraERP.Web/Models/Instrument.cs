using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Instrument
{
    [Required, StringLength(20)] public string Code { get; set; } = "INST-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string Type { get; set; } = "Analytical";
    public DateTime LastCalibration { get; set; } = DateTime.Today;
    public DateTime NextCalibration { get; set; } = DateTime.Today.AddMonths(6);
    [StringLength(20)] public string Status { get; set; } = "Operational";
}
