using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class MigoModel : PageModel
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly YuktiraERP.Core.Interfaces.IMovementTypeEngineService _engineService;

    public MigoModel(YuktiraDbContext db, ITenantContext tenant, YuktiraERP.Core.Interfaces.IMovementTypeEngineService engineService)
    {
        _db = db;
        _tenant = tenant;
        _engineService = engineService;
    }

    [BindProperty]
    public MovementPostRequest Request { get; set; } = new()
    {
        PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        Lines = new List<MovementPostLineRequest> { new() }
    };

    [BindProperty]
    public string? ReverseDocumentId { get; set; }
    [BindProperty]
    public string? ReverseReason { get; set; }

    public string? Error { get; set; }
    public string? SuccessMessage { get; set; }
    public MovementValidationResult? ValidationResult { get; set; }
    public MovementWorkflowSimulationResult? SimulationResult { get; set; }
    public MovementPostResult? PostResult { get; set; }
    public List<SelectListItem> MovementTypeOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadMovementTypes();
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        await LoadMovementTypes();
        Request.UserId = _tenant.TenantId.ToString();

        ValidationResult = await _engineService.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = Request.MovementType,
            SpecialStockIndicator = Request.SpecialStockIndicator,
            Plant = Request.Plant,
            StorageLocation = Request.StorageLocation,
            Quantity = Request.Lines?.Sum(l => l.Quantity) ?? 0,
            MaterialCode = Request.Lines?.FirstOrDefault()?.MaterialCode ?? "",
            StockType = Request.Lines?.FirstOrDefault()?.StockType ?? "FREE",
            Reference = Request.Reference,
            TenantId = _tenant.TenantId
        });

        return Page();
    }

    public async Task<IActionResult> OnPostSimulateAsync()
    {
        await LoadMovementTypes();
        Request.UserId = _tenant.TenantId.ToString();

        SimulationResult = await _engineService.SimulateWorkflowAsync(new MovementSimulationRequest
        {
            MovementType = Request.MovementType,
            SpecialStockIndicator = Request.SpecialStockIndicator,
            MaterialCode = Request.Lines?.FirstOrDefault()?.MaterialCode ?? "",
            Quantity = Request.Lines?.Sum(l => l.Quantity) ?? 0,
            Plant = Request.Plant,
            StorageLocation = Request.StorageLocation,
            StockType = Request.Lines?.FirstOrDefault()?.StockType ?? "FREE",
            TenantId = _tenant.TenantId
        });

        return Page();
    }

    public async Task<IActionResult> OnPostPostAsync()
    {
        await LoadMovementTypes();
        Request.TenantId = _tenant.TenantId;
        Request.UserId = User.Identity?.Name ?? "SYSTEM";

        if (Request.Lines == null || !Request.Lines.Any())
        {
            Error = "At least one line item is required.";
            return Page();
        }

        PostResult = await _engineService.PostMovementAsync(Request);

        if (!PostResult.Success)
        {
            Error = string.Join("; ", PostResult.Errors);
        }
        else
        {
            SuccessMessage = $"Document {PostResult.DocumentNumber} posted successfully.";
            Request = new MovementPostRequest
            {
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Lines = new List<MovementPostLineRequest> { new() }
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReverseAsync()
    {
        await LoadMovementTypes();

        if (string.IsNullOrEmpty(ReverseDocumentId) || !Guid.TryParse(ReverseDocumentId, out var docId))
        {
            Error = "Invalid document ID.";
            return Page();
        }

        var result = await _engineService.ReverseMovementAsync(docId, ReverseReason ?? "Manual reversal", User.Identity?.Name ?? "SYSTEM");

        if (!result.Success)
        {
            Error = string.Join("; ", result.Errors);
        }
        else
        {
            SuccessMessage = $"Document reversed. New document: {result.DocumentNumber}";
        }

        return Page();
    }

    private async Task LoadMovementTypes()
    {
        var types = await _engineService.GetAllMovementTypesAsync(_tenant.TenantId);
        MovementTypeOptions = types
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key)
            .SelectMany(g => g.OrderBy(t => t.MovementType)
                .Select(t => new SelectListItem
                {
                    Value = t.MovementType.ToString(),
                    Text = $"{t.MovementType} - {t.Description}"
                }))
            .ToList();
    }
}
