using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/sd/[controller]")]
[Authorize]
public class QuotationController : ModuleCrudControllerBase<QuotationEntity>
{
    public QuotationController(IRepository<QuotationEntity, Guid> repo, ITenantContext tenant) : base(repo, tenant) { }
}
