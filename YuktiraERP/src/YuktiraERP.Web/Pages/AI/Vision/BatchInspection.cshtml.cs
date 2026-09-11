using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.AI.Vision;

[Authorize]
public class BatchInspectionModel : PageModel
{
    [BindProperty]
    public List<IFormFile>? ImageFiles { get; set; }

    [BindProperty]
    public string MaterialCode { get; set; } = "";

    [BindProperty]
    public string Plant { get; set; } = "";

    [BindProperty]
    public string InspectionLotNumber { get; set; } = "";

    public List<BatchResultVm> Results { get; set; } = new();
    public int TotalCount => Results.Count;
    public int PassedCount => Results.Count(r => r.IsPassed);
    public int FailedCount => Results.Count(r => !r.IsPassed && r.Severity != "Warning");
    public int WarningCount => Results.Count(r => r.Severity == "Warning");
    public double PassRate => TotalCount > 0 ? (double)PassedCount / TotalCount * 100 : 0;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ImageFiles == null || !ImageFiles.Any())
        {
            ModelState.AddModelError("ImageFiles", "Please upload at least one image.");
            return Page();
        }

        var random = new Random();
        var severities = new[] { "Low", "Medium", "High", "Critical" };
        var defects = new[] { "Surface Scratch", "Dent", "Discoloration", "Crack", "Contamination", "None" };
        var actions = new[] { "Accept", "Reject", "Rework", "Manual Review" };

        Results = new List<BatchResultVm>();
        foreach (var file in ImageFiles)
        {
            var hasDefect = random.NextDouble() > 0.4;
            var severity = hasDefect ? severities[random.Next(severities.Length - 1) + 1] : "Low";
            var defect = hasDefect ? defects[random.Next(defects.Length - 1)] : "None";
            var confidence = 85.0 + random.NextDouble() * 14.0;
            var isPassed = !hasDefect || severity == "Low";

            Results.Add(new BatchResultVm
            {
                ImageName = file.FileName,
                IsPassed = isPassed,
                DefectType = defect,
                ConfidencePct = confidence,
                Severity = severity,
                NCRequired = hasDefect && (severity == "High" || severity == "Critical")
            });
        }

        return Page();
    }
}

public class BatchResultVm
{
    public string ImageName { get; set; } = "";
    public bool IsPassed { get; set; }
    public string DefectType { get; set; } = "";
    public double ConfidencePct { get; set; }
    public string Severity { get; set; } = "";
    public bool NCRequired { get; set; }
}
