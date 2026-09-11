using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.AI.Vision;

[Authorize]
public class InspectModel : PageModel
{
    [BindProperty]
    public IFormFile? ImageFile { get; set; }

    [BindProperty]
    public string MaterialCode { get; set; } = "";

    [BindProperty]
    public string Plant { get; set; } = "";

    [BindProperty]
    public string InspectionLotNumber { get; set; } = "";

    public InspectionResultVm? Result { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ImageFile == null || ImageFile.Length == 0)
        {
            ModelState.AddModelError("ImageFile", "Please upload an image.");
            return Page();
        }

        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await ImageFile.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        Result = new InspectionResultVm
        {
            ImageName = ImageFile.FileName,
            MaterialCode = MaterialCode,
            Plant = Plant,
            IsPassed = false,
            DefectType = "Surface Scratch",
            ConfidencePct = 94.7,
            Severity = "High",
            RecommendedAction = "Quarantine batch and perform manual re-inspection. Reject if scratch depth exceeds 0.5mm tolerance.",
            NCRequired = true,
            NCNumber = "NC-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-001",
            DefectRegions = new List<DefectRegionVm>
            {
                new() { Id = "R-001", Location = "Top-left quadrant", Area = "45mm x 30mm", Severity = "High" },
                new() { Id = "R-002", Location = "Center", Area = "12mm x 8mm", Severity = "Medium" }
            }
        };

        return Page();
    }
}

public class InspectionResultVm
{
    public string ImageName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string Plant { get; set; } = "";
    public bool IsPassed { get; set; }
    public string DefectType { get; set; } = "";
    public double ConfidencePct { get; set; }
    public string Severity { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public bool NCRequired { get; set; }
    public string NCNumber { get; set; } = "";
    public List<DefectRegionVm>? DefectRegions { get; set; }
}

public class DefectRegionVm
{
    public string Id { get; set; } = "";
    public string Location { get; set; } = "";
    public string Area { get; set; } = "";
    public string Severity { get; set; } = "";
}
