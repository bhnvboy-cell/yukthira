using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.AI.Vision;

[Authorize]
public class HistoryModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string MaterialCode { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string Plant { get; set; } = "";

    public List<HistoryRecordVm> Records { get; set; } = new();

    public void OnGet()
    {
        if (!FromDate.HasValue)
            FromDate = DateTime.Today.AddDays(-30);
        if (!ToDate.HasValue)
            ToDate = DateTime.Today;

        var rnd = new Random();
        var defects = new[] { "Surface Scratch", "Dent", "Discoloration", "Crack", "Contamination", "None" };
        var severities = new[] { "Low", "Medium", "High", "Critical" };
        var plants = new[] { "1000", "2000", "3000" };
        var materials = new[] { "MAT-001", "MAT-002", "MAT-003", "MAT-004", "MAT-005" };
        var count = 8 + rnd.Next(12);

        for (int i = 0; i < count; i++)
        {
            var date = FromDate.Value.AddDays(rnd.Next((ToDate.Value - FromDate.Value).Days + 1));
            var hasDefect = rnd.NextDouble() > 0.4;
            var severity = hasDefect ? severities[rnd.Next(1, severities.Length)] : "Low";
            var mat = string.IsNullOrEmpty(MaterialCode) ? materials[rnd.Next(materials.Length)] : MaterialCode;
            var plnt = string.IsNullOrEmpty(Plant) ? plants[rnd.Next(plants.Length)] : Plant;

            Records.Add(new HistoryRecordVm
            {
                InspectionDate = date.AddHours(rnd.Next(6, 18)).AddMinutes(rnd.Next(0, 60)),
                MaterialCode = mat,
                Plant = plnt,
                DefectType = hasDefect ? defects[rnd.Next(defects.Length - 1)] : "None",
                Severity = severity,
                ConfidencePct = 85.0 + rnd.NextDouble() * 14.0,
                NCNumber = hasDefect && (severity == "High" || severity == "Critical") ? "NC-" + date.ToString("yyyyMMdd") + "-" + rnd.Next(100, 999).ToString() : "",
                Status = hasDefect ? "Fail" : "Pass"
            });
        }

        Records = Records.OrderByDescending(r => r.InspectionDate).ToList();
    }
}

public class HistoryRecordVm
{
    public DateTime InspectionDate { get; set; }
    public string MaterialCode { get; set; } = "";
    public string Plant { get; set; } = "";
    public string DefectType { get; set; } = "";
    public string Severity { get; set; } = "";
    public double ConfidencePct { get; set; }
    public string NCNumber { get; set; } = "";
    public string Status { get; set; } = "";
}
