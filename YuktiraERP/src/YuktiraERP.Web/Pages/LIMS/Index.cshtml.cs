using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.LIMS;

public class IndexModel : PageModel
{
    private readonly IRepository<SampleEntity, Guid> _sampleRepo;
    private readonly IRepository<SpecificationEntity, Guid> _specRepo;
    private readonly IRepository<InstrumentEntity, Guid> _instrumentRepo;

    public List<SampleEntity> Samples { get; set; } = new();
    public List<SpecificationEntity> Specifications { get; set; } = new();
    public List<InstrumentEntity> Instruments { get; set; } = new();

    public IndexModel(
        IRepository<SampleEntity, Guid> sampleRepo,
        IRepository<SpecificationEntity, Guid> specRepo,
        IRepository<InstrumentEntity, Guid> instrumentRepo)
    {
        _sampleRepo = sampleRepo;
        _specRepo = specRepo;
        _instrumentRepo = instrumentRepo;
    }

    public async Task OnGetAsync()
    {
        Samples = await _sampleRepo.GetAllAsync();
        Specifications = await _specRepo.GetAllAsync();
        Instruments = await _instrumentRepo.GetAllAsync();
    }
}
