using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.QM.InspectionLot;

[Authorize]
public class SelectModel : PageModel
{
    private readonly IQualityManagementService _qmService;
    private readonly ITenantContext _tenant;

    public SelectModel(IQualityManagementService qmService, ITenantContext tenant)
    {
        _qmService = qmService;
        _tenant = tenant;
    }

    [BindProperty]
    public InspectionLotSelectionFilterDto Filter { get; set; } = new();

    public InspectionLotSelectionResponseDto? Results { get; set; }
    public bool HasSearched { get; set; }

    public void OnGet()
    {
        Filter.MaxHits = 100;
        Filter.UsageDecisionFilter = UsageDecisionFilter.AllLots;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Filter.MaxHits <= 0) Filter.MaxHits = 100;
        if (Filter.MaxHits > 500) Filter.MaxHits = 500;
        Results = await _qmService.GetSelectedInspectionLotsAsync(Filter, _tenant.TenantId);
        HasSearched = true;
        return Page();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        Filter = new InspectionLotSelectionFilterDto { MaxHits = 100, UsageDecisionFilter = UsageDecisionFilter.AllLots };
        HasSearched = false;
        return Page();
    }
}
