using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class Sample
{
    [Required, StringLength(20)] public string SampleId { get; set; } = "SMP-" + Guid.NewGuid().ToString()[..4];
    [Required, StringLength(100)] public string MaterialName { get; set; } = string.Empty;
    [StringLength(50)] public string Source { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; } = DateTime.Today;
    public int TestCount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Submitted";
}
