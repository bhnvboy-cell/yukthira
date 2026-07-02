using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Employee;
public class EditModel : PageModel
{
    private readonly IRepository<EmployeeEntity, Guid> _repo;
    public EditModel(IRepository<EmployeeEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public EmployeeEntity Employee { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Employee = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Employee);
        return RedirectToPage("/HR/Index");
    }
}
