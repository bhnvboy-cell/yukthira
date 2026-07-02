using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM;

public class IndexModel : PageModel
{
    private readonly IRepository<LeadEntity, Guid> _leadRepo;
    private readonly IRepository<ContactEntity, Guid> _contactRepo;
    private readonly IRepository<CampaignEntity, Guid> _campaignRepo;
    private readonly IRepository<ServiceTicketEntity, Guid> _ticketRepo;

    public List<LeadEntity> Leads { get; set; } = new();
    public List<ContactEntity> Contacts { get; set; } = new();
    public List<CampaignEntity> Campaigns { get; set; } = new();
    public List<ServiceTicketEntity> ServiceTickets { get; set; } = new();

    public IndexModel(
        IRepository<LeadEntity, Guid> leadRepo,
        IRepository<ContactEntity, Guid> contactRepo,
        IRepository<CampaignEntity, Guid> campaignRepo,
        IRepository<ServiceTicketEntity, Guid> ticketRepo)
    {
        _leadRepo = leadRepo;
        _contactRepo = contactRepo;
        _campaignRepo = campaignRepo;
        _ticketRepo = ticketRepo;
    }

    public async Task OnGetAsync()
    {
        Leads = await _leadRepo.GetAllAsync();
        Contacts = await _contactRepo.GetAllAsync();
        Campaigns = await _campaignRepo.GetAllAsync();
        ServiceTickets = await _ticketRepo.GetAllAsync();
    }
}
