using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Web.Pages.QM.Inspection;

[Authorize]
public class ReverseUDModel : PageModel
{
    private readonly IInspectionResultService _inspectionService;
    private readonly ITenantContext _tenant;
    private readonly YuktiraDbContext _db;

    public ReverseUDModel(IInspectionResultService inspectionService, ITenantContext tenant, YuktiraDbContext db)
    {
        _inspectionService = inspectionService;
        _tenant = tenant;
        _db = db;
    }

    [BindProperty]
    public string LotIdInput { get; set; } = "";

    [BindProperty]
    public string ReversalReason { get; set; } = "";

    public InspectionLotEntity? FoundLot { get; set; }
    public UsageDecisionEntity? CurrentUD { get; set; }
    public UDReversalResult? ReversalResult { get; set; }
    public List<StockBalanceEntity> StockBalances { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        await LoadStockBalancesAsync();

        if (string.IsNullOrWhiteSpace(LotIdInput))
        {
            ErrorMessage = "Please enter an Inspection Lot ID.";
            return Page();
        }

        if (!Guid.TryParse(LotIdInput, out var lotId))
        {
            ErrorMessage = $"Invalid Inspection Lot ID format: '{LotIdInput}'.";
            return Page();
        }

        FoundLot = await _inspectionService.GetInspectionLotByIdAsync(lotId);
        if (FoundLot == null)
        {
            ErrorMessage = $"Inspection lot '{LotIdInput}' not found.";
            return Page();
        }

        CurrentUD = null;
        CurrentUD = await _db.UsageDecisions.FirstOrDefaultAsync(u => u.LotNumber == FoundLot.LotNumber);

        return Page();
    }

    public async Task<IActionResult> OnPostReverseAsync()
    {
        await LoadStockBalancesAsync();

        if (!Guid.TryParse(LotIdInput, out var lotId))
        {
            ErrorMessage = "Invalid Inspection Lot ID.";
            return Page();
        }

        FoundLot = await _inspectionService.GetInspectionLotByIdAsync(lotId);
        if (FoundLot == null)
        {
            ErrorMessage = $"Inspection lot '{LotIdInput}' not found.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ReversalReason))
        {
            ErrorMessage = "Reversal reason is required.";
            return Page();
        }

        ReversalResult = await _inspectionService.ReverseUsageDecisionAsync(
            lotId, ReversalReason, _tenant.TenantId, User.Identity?.Name ?? "SYSTEM");

        if (ReversalResult.Success)
        {
            SuccessMessage = $"UD Reversal completed successfully for lot {ReversalResult.InspectionLotNumber}. " +
                           $"Stock {ReversalResult.StockQuantityMoved} units moved from {ReversalResult.StockFromType} to {ReversalResult.StockToType} (Movement Type {ReversalResult.StockMovementType}).";

            FoundLot = await _inspectionService.GetInspectionLotByIdAsync(lotId);
            await LoadStockBalancesAsync();
        }
        else
        {
            ErrorMessage = string.Join(" ", ReversalResult.Errors);
        }

        return Page();
    }

    private async Task LoadStockBalancesAsync()
    {
        StockBalances = await _inspectionService.GetStockBalancesAsync(_tenant.TenantId);
    }
}
