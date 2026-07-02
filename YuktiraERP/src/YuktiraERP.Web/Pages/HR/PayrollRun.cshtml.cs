using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR;
public class PayrollRunModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    private readonly IRepository<EmployeeEntity, Guid> _empRepo;
    public PayrollRunModel(IRepository<PayrollEntryEntity, Guid> repo, IRepository<EmployeeEntity, Guid> empRepo) { _repo = repo; _empRepo = empRepo; }
    public List<PayrollEntryEntity> PayrollEntries { get; set; } = new();
    public string? Message { get; set; }
    public async Task OnGetAsync() { PayrollEntries = await _repo.GetAllAsync(); }
    public async Task<IActionResult> OnPostRunAsync()
    {
        var employees = await _empRepo.GetAllAsync();
        var period = DateTime.Now.ToString("yyyy-MM");
        foreach (var emp in employees.Where(e => e.Status == "Active"))
        {
            var entry = new PayrollEntryEntity
            {
                PayrollId = $"PR-{DateTime.Now.Ticks}",
                EmployeeName = emp.Name,
                Period = period,
                GrossPay = 5000, Deductions = 1000, NetPay = 4000,
                Status = "Draft"
            };
            await _repo.AddAsync(entry);
        }
        Message = $"Payroll run completed for period {period}. {employees.Count(e => e.Status == "Active")} employees processed.";
        PayrollEntries = await _repo.GetAllAsync();
        return Page();
    }
}
