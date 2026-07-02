using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS;

public class IndexModel : PageModel
{
    private readonly IRepository<ProjectEntity, Guid> _projRepo;
    private readonly IRepository<ProjectTaskEntity, Guid> _taskRepo;
    private readonly IRepository<TimesheetEntryEntity, Guid> _tsRepo;

    public List<ProjectEntity> Projects { get; set; } = new();
    public List<ProjectTaskEntity> Tasks { get; set; } = new();
    public List<TimesheetEntryEntity> Timesheets { get; set; } = new();

    public IndexModel(
        IRepository<ProjectEntity, Guid> projRepo,
        IRepository<ProjectTaskEntity, Guid> taskRepo,
        IRepository<TimesheetEntryEntity, Guid> tsRepo)
    {
        _projRepo = projRepo;
        _taskRepo = taskRepo;
        _tsRepo = tsRepo;
    }

    public async Task OnGetAsync()
    {
        Projects = await _projRepo.GetAllAsync();
        Tasks = await _taskRepo.GetAllAsync();
        Timesheets = await _tsRepo.GetAllAsync();
    }
}
