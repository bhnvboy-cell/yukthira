using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.SerialNumber;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<SerialNumberEntity, Guid> _serialRepo;
    private readonly IRepository<MaterialMasterEntity, Guid> _materialRepo;
    private readonly IRepository<BatchEntity, Guid> _batchRepo;

    public CreateModel(
        IRepository<SerialNumberEntity, Guid> serialRepo,
        IRepository<MaterialMasterEntity, Guid> materialRepo,
        IRepository<BatchEntity, Guid> batchRepo)
    {
        _serialRepo = serialRepo;
        _materialRepo = materialRepo;
        _batchRepo = batchRepo;
    }

    [BindProperty] public SerialNumberEntity Serial { get; set; } = new();
    public List<MaterialMasterEntity> Materials { get; set; } = new();
    public List<BatchEntity> Batches { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Materials = await _materialRepo.GetAllAsync();
        Batches = await _batchRepo.GetAllAsync();
        Serial.ManufacturingDate = DateTime.UtcNow;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _serialRepo.AddAsync(Serial);
        return RedirectToPage("/MM/SerialNumber/List");
    }
}
