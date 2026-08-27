using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Web.Pages.MM.PR;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    private readonly IApprovalWorkflowService _approvalService;
    public DisplayModel(IRepository<PurchaseRequisitionEntity, Guid> repo, YuktiraDbContext db, IApprovalWorkflowService approvalService) { _repo = repo; _db = db; _approvalService = approvalService; }
    public PurchaseRequisitionEntity Requisition { get; set; } = new();
    public List<PurchaseRequisitionItemEntity> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/PR/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Requisition = entity;

        Items = await _db.PurchaseRequisitionItems
            .Where(i => i.PurchaseRequisitionId == id.Value)
            .OrderBy(i => i.LineNumber)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitForApprovalAsync(Guid prId)
    {
        var userId = User.Identity?.Name ?? "system";
        try
        {
            await _approvalService.SubmitPrForApprovalAsync(prId, userId);
            TempData["Success"] = "PR submitted for approval.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { id = prId });
    }
}
