using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/crm")]
[Authorize]
public class CRMController : ControllerBase
{
    private readonly IRepository<LeadEntity, Guid> _leads;
    private readonly IRepository<OpportunityEntity, Guid> _opportunities;
    private readonly IRepository<ContactEntity, Guid> _contacts;
    private readonly IRepository<CampaignEntity, Guid> _campaigns;
    private readonly IRepository<ServiceTicketEntity, Guid> _tickets;
    private readonly ITenantContext _tenant;

    public CRMController(
        IRepository<LeadEntity, Guid> leads,
        IRepository<OpportunityEntity, Guid> opportunities,
        IRepository<ContactEntity, Guid> contacts,
        IRepository<CampaignEntity, Guid> campaigns,
        IRepository<ServiceTicketEntity, Guid> tickets,
        ITenantContext tenant)
    {
        _leads = leads;
        _opportunities = opportunities;
        _contacts = contacts;
        _campaigns = campaigns;
        _tickets = tickets;
        _tenant = tenant;
    }

    [HttpGet("leads")] public async Task<IActionResult> GetLeads() => Ok(new { data = await _leads.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("leads")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateLead([FromBody] LeadEntity model) { model.Id = Guid.NewGuid(); await _leads.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("opportunities")] public async Task<IActionResult> GetOpportunities() => Ok(new { data = await _opportunities.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("opportunities")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityEntity model) { model.Id = Guid.NewGuid(); await _opportunities.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("contacts")] public async Task<IActionResult> GetContacts() => Ok(new { data = await _contacts.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("contacts")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateContact([FromBody] ContactEntity model) { model.Id = Guid.NewGuid(); await _contacts.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("campaigns")] public async Task<IActionResult> GetCampaigns() => Ok(new { data = await _campaigns.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("campaigns")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCampaign([FromBody] CampaignEntity model) { model.Id = Guid.NewGuid(); await _campaigns.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("service-tickets")] public async Task<IActionResult> GetServiceTickets() => Ok(new { data = await _tickets.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("service-tickets")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateServiceTicket([FromBody] ServiceTicketEntity model) { model.Id = Guid.NewGuid(); await _tickets.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
