using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mm/[controller]")]
[Authorize]
public class PRController : ModuleCrudControllerBase<PurchaseRequisitionEntity>
{
    public PRController(IRepository<PurchaseRequisitionEntity, Guid> repo, ITenantContext tenant) : base(repo, tenant) { }
}
