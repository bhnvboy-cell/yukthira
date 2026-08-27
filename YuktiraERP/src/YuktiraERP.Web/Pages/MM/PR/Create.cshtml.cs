using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
namespace YuktiraERP.Web.Pages.MM.PR;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _repo;
    private readonly IRepository<PurchaseRequisitionItemEntity, Guid> _itemRepo;
    private readonly IDepartmentKeyService _deptService;
    private readonly YuktiraDbContext _db;
    public CreateModel(IRepository<PurchaseRequisitionEntity, Guid> repo, IRepository<PurchaseRequisitionItemEntity, Guid> itemRepo, IDepartmentKeyService deptService, YuktiraDbContext db)
    { _repo = repo; _itemRepo = itemRepo; _deptService = deptService; _db = db; }

    [BindProperty] public PurchaseRequisitionEntity Requisition { get; set; } = new();
    [BindProperty] public List<PurchaseRequisitionItemEntity> LineItems { get; set; } = new() { new PurchaseRequisitionItemEntity() };
    public List<SelectListItem> DepartmentKeyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var tenantId = _db.TenantId ?? Guid.Empty;
        var depts = await _deptService.GetAllAsync(tenantId);
        DepartmentKeyOptions = depts.Select(d => new SelectListItem { Value = d.Code, Text = $"{d.Code} - {d.Name}" }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var tenantId = _db.TenantId ?? Guid.Empty;
        Requisition.TenantId = tenantId;
        Requisition.TotalAmount = LineItems.Where(l => l.MaterialName != "").Sum(l => l.Quantity * l.UnitPrice);
        Requisition.ItemCount = LineItems.Count(l => l.MaterialName != "");
        await _repo.AddAsync(Requisition);

        int lineNum = 1;
        foreach (var item in LineItems.Where(l => l.MaterialName != ""))
        {
            item.Id = Guid.NewGuid();
            item.TenantId = tenantId;
            item.PurchaseRequisitionId = Requisition.Id;
            item.LineNumber = lineNum++;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            item.Status = "OPEN";
            _db.PurchaseRequisitionItems.Add(item);
        }
        await _db.SaveChangesAsync();
        return RedirectToPage("/MM/PR/List");
    }
}
