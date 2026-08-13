using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Payroll;

public class CreateModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    private readonly IPayrollService _payroll;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<PayrollEntryEntity, Guid> repo, IPayrollService payroll, ITenantContext tenant)
    {
        _repo = repo;
        _payroll = payroll;
        _tenant = tenant;
    }

    [BindProperty] public PayrollEntryEntity Entry { get; set; } = new();
    [BindProperty] public PayrollCalculationRequest Calculation { get; set; } = new();
    public PayrollCalculationResult? Result { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _payroll.CalculatePayrollAsync(Calculation, Entry.Period);

        Entry.TenantId = _tenant.TenantId;
        Entry.PayrollId = string.IsNullOrEmpty(Entry.PayrollId) ? "PAY-" + DateTime.Now.Ticks : Entry.PayrollId;
        Entry.EmployeeName = string.IsNullOrEmpty(Entry.EmployeeName) ? Calculation.EmployeeName : Entry.EmployeeName;
        Entry.Period = string.IsNullOrEmpty(Entry.Period) ? $"{DateTime.Today:MMMM yyyy}" : Entry.Period;
        Entry.GrossPay = result.GrossPay;
        Entry.Deductions = result.TotalDeductions;
        Entry.NetPay = result.NetPay;
        Entry.Status = string.IsNullOrEmpty(Entry.Status) ? "Posted" : Entry.Status;

        await _repo.AddAsync(Entry);
        return RedirectToPage("/HR/Payroll/List");
    }
}