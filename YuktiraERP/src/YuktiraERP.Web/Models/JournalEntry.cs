using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class JournalEntry
{
    [Required, StringLength(20)] public string DocumentNumber { get; set; } = "GL-" + Guid.NewGuid().ToString()[..4];
    public DateTime EntryDate { get; set; } = DateTime.Today;
    [StringLength(50)] public string Account { get; set; } = string.Empty;
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    [StringLength(100)] public string Reference { get; set; } = string.Empty;
}
