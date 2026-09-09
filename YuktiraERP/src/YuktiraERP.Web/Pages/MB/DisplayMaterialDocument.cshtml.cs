using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class DisplayMaterialDocumentModel : PageModel
{
    private readonly IInventoryMovementService _service;
    private readonly ITenantContext _tenant;

    public DisplayMaterialDocumentModel(IInventoryMovementService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public string? DocumentNumber { get; set; }

    public Mb51DocumentDto? Document { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(DocumentNumber))
        {
            Document = await _service.GetMaterialDocumentAsync(DocumentNumber.Trim(), _tenant.TenantId);
            if (Document == null)
                Error = $"Document {DocumentNumber} not found.";
        }
    }
}
