using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<BatchEntity, Guid> _batchRepo;
    private readonly IRepository<MaterialMasterEntity, Guid> _materialRepo;
    private readonly IRepository<StorageLocationEntity, Guid> _locationRepo;
    private readonly IRepository<VendorEntity, Guid> _vendorRepo;

    public CreateModel(
        IRepository<BatchEntity, Guid> batchRepo,
        IRepository<MaterialMasterEntity, Guid> materialRepo,
        IRepository<StorageLocationEntity, Guid> locationRepo,
        IRepository<VendorEntity, Guid> vendorRepo)
    {
        _batchRepo = batchRepo;
        _materialRepo = materialRepo;
        _locationRepo = locationRepo;
        _vendorRepo = vendorRepo;
    }

    [BindProperty] public BatchEntity Batch { get; set; } = new();
    public List<MaterialMasterEntity> Materials { get; set; } = new();
    public List<StorageLocationEntity> Locations { get; set; } = new();
    public List<VendorEntity> Vendors { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Materials = await _materialRepo.GetAllAsync();
        Locations = await _locationRepo.GetAllAsync();
        Vendors = await _vendorRepo.GetAllAsync();
        Batch.ManufacturingDate = DateTime.UtcNow;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _batchRepo.AddAsync(Batch);
        return RedirectToPage("/MM/Batch/List");
    }
}
