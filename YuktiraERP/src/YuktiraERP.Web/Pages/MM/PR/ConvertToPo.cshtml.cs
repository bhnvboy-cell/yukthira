using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Web.Pages.MM.PR;

[Authorize]
public class ConvertToPoModel : PageModel
{
    private readonly IPrPoConversionService _conversionService;
    public ConvertToPoModel(IPrPoConversionService conversionService) { _conversionService = conversionService; }

    public PrPoConversionResult? Preview { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/PR/List");
        try
        {
            Preview = await _conversionService.GetConversionPreviewAsync(id.Value);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/MM/PR/List");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid prId)
    {
        var userId = User.Identity?.Name ?? "system";
        var request = new ConvertPrToPoRequest
        {
            VendorName = Request.Form["VendorName"].FirstOrDefault() ?? "",
            VendorCode = Request.Form["VendorCode"].FirstOrDefault() ?? "",
            PaymentTerms = Request.Form["PaymentTerms"].FirstOrDefault() ?? "Net 30",
            Incoterms = Request.Form["Incoterms"].FirstOrDefault() ?? "",
            DeliveryDate = Request.Form["DeliveryDate"].FirstOrDefault() ?? "",
            Plant = Request.Form["Plant"].FirstOrDefault() ?? "",
            Notes = Request.Form["Notes"].FirstOrDefault() ?? "",
            SelectedItemIds = Request.Form["SelectedItemIds"].Select(Guid.Parse).ToList()
        };

        try
        {
            var po = await _conversionService.ConvertPrToPoAsync(prId, request, userId);
            TempData["Success"] = $"PO {po.PoNumber} created successfully from PR.";
            return RedirectToPage("/MM/PO/Display", new { id = po.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage(new { id = prId });
        }
    }
}
