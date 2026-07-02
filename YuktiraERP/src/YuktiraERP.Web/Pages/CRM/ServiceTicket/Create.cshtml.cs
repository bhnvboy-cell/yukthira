using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.ServiceTicket;
public class CreateModel : PageModel
{
    private readonly IRepository<ServiceTicketEntity, Guid> _repo;
    public CreateModel(IRepository<ServiceTicketEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ServiceTicketEntity Ticket { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Ticket); return RedirectToPage("/CRM/Index"); }
}
