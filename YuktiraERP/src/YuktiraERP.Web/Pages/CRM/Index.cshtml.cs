using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<LeadEntity, Guid> _leadRepo;
    private readonly IRepository<OpportunityEntity, Guid> _opportunityRepo;
    private readonly IRepository<ContactEntity, Guid> _contactRepo;
    private readonly IRepository<CampaignEntity, Guid> _campaignRepo;
    private readonly IRepository<ServiceTicketEntity, Guid> _ticketRepo;
    private readonly IRepository<CrmAccountEntity, Guid> _accountRepo;
    private readonly IRepository<SalesPipelineEntity, Guid> _pipelineRepo;

    public List<LeadEntity> Leads { get; set; } = new();
    public List<OpportunityEntity> Opportunities { get; set; } = new();
    public List<ContactEntity> Contacts { get; set; } = new();
    public List<CampaignEntity> Campaigns { get; set; } = new();
    public List<ServiceTicketEntity> ServiceTickets { get; set; } = new();
    public List<CrmAccountEntity> Accounts { get; set; } = new();
    public List<SalesPipelineEntity> SalesPipelines { get; set; } = new();

    public decimal TotalOpenPipelineValue { get; set; }
    public decimal ConversionRate { get; set; }
    public int WonOpportunitiesYTD { get; set; }
    public decimal AverageDealSize { get; set; }
    public int OpenSupportTickets { get; set; }

    public IndexModel(
        IRepository<LeadEntity, Guid> leadRepo,
        IRepository<OpportunityEntity, Guid> opportunityRepo,
        IRepository<ContactEntity, Guid> contactRepo,
        IRepository<CampaignEntity, Guid> campaignRepo,
        IRepository<ServiceTicketEntity, Guid> ticketRepo,
        IRepository<CrmAccountEntity, Guid> accountRepo,
        IRepository<SalesPipelineEntity, Guid> pipelineRepo)
    {
        _leadRepo = leadRepo;
        _opportunityRepo = opportunityRepo;
        _contactRepo = contactRepo;
        _campaignRepo = campaignRepo;
        _ticketRepo = ticketRepo;
        _accountRepo = accountRepo;
        _pipelineRepo = pipelineRepo;
    }

    public async Task OnGetAsync()
    {
        Leads = await _leadRepo.GetAllAsync();
        Opportunities = await _opportunityRepo.GetAllAsync();
        Contacts = await _contactRepo.GetAllAsync();
        Campaigns = await _campaignRepo.GetAllAsync();
        ServiceTickets = await _ticketRepo.GetAllAsync();
        Accounts = await _accountRepo.GetAllAsync();
        SalesPipelines = await _pipelineRepo.GetAllAsync();

        TotalOpenPipelineValue = SalesPipelines
            .Where(p => p.Status == "Open")
            .Sum(p => p.DealValue);

        var totalOpps = Opportunities.Count;
        var wonOpps = Opportunities.Count(o => o.Stage == "Closed Won");
        ConversionRate = totalOpps > 0 ? Math.Round((decimal)wonOpps / totalOpps * 100, 1) : 0;

        WonOpportunitiesYTD = Opportunities
            .Count(o => o.Stage == "Closed Won" && o.CreatedAt.Year == DateTime.Now.Year);

        AverageDealSize = Opportunities.Count > 0
            ? Math.Round(Opportunities.Average(o => o.Value), 2)
            : 0;

        OpenSupportTickets = ServiceTickets.Count(t => t.Status == "Open");
    }
}
