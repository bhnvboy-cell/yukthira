using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.GRN;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<GoodsReceiptEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    public EditModel(IRepository<GoodsReceiptEntity, Guid> repo, YuktiraDbContext db) { _repo = repo; _db = db; }
    [BindProperty] public GoodsReceiptEntity Receipt { get; set; } = new();
    public string BatchNumber { get; set; } = "";
    public string StorageLocation { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/GRN/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Receipt = entity;

        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.BatchNumber == Receipt.GrnNumber);
        if (batch != null)
        {
            BatchNumber = batch.BatchNumber;
            StorageLocation = batch.StorageLocationName;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Receipt);
        return RedirectToPage("/MM/GRN/List");
    }
}
