using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class GoodsIssueModel : PageModel
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public GoodsIssueModel(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [BindProperty] public string Reference { get; set; } = "";
    [BindProperty] public string MaterialName { get; set; } = "";
    [BindProperty] public decimal Quantity { get; set; }
    [BindProperty] public string Reason { get; set; } = "Production";

    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(MaterialName) || Quantity <= 0)
        {
            Error = "Material and a positive quantity are required.";
            return Page();
        }

        var material = await _db.MaterialMasters
            .FirstOrDefaultAsync(m => m.Name == MaterialName || m.Code == MaterialName);
        if (material == null)
        {
            Error = $"Material '{MaterialName}' not found.";
            return Page();
        }

        if (material.Stock < Quantity)
        {
            Error = $"Insufficient stock for '{MaterialName}'. Available: {material.Stock} {material.UOM}.";
            return Page();
        }

        var before = material.Stock;
        material.Stock -= Quantity;
        material.UpdatedAt = DateTime.UtcNow;

        _db.StockMovements.Add(new StockMovementEntity
        {
            TenantId = _tenant.TenantId,
            DocumentNumber = "GI-" + DateTime.Now.Ticks,
            MaterialName = material.Name,
            MovementType = "Issue",
            Quantity = Quantity,
            StockBefore = before,
            StockAfter = material.Stock,
            Reference = string.IsNullOrEmpty(Reference) ? Reason : $"{Reason} ({Reference})",
            Status = "Posted"
        });

        await _db.SaveChangesAsync();
        return RedirectToPage("/MM/Index");
    }
}