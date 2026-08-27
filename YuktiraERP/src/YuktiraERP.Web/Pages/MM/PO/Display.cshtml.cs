using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
namespace YuktiraERP.Web.Pages.MM.PO;
[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<PurchaseOrderEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    private readonly IApprovalWorkflowService _approvalService;
    public DisplayModel(IRepository<PurchaseOrderEntity, Guid> repo, YuktiraDbContext db, IApprovalWorkflowService approvalService) { _repo = repo; _db = db; _approvalService = approvalService; }
    public PurchaseOrderEntity Order { get; set; } = new();
    public List<PurchaseOrderItemEntity> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Order = entity;

        Items = await _db.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == id.Value)
            .OrderBy(i => i.LineNumber)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitForApprovalAsync(Guid poId)
    {
        var userId = User.Identity?.Name ?? "system";
        try
        {
            await _approvalService.SubmitPoForApprovalAsync(poId, userId);
            TempData["Success"] = "PO submitted for approval.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { id = poId });
    }
}
