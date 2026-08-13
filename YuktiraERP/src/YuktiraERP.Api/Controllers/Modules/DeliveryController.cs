using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/sd/[controller]")]
[Authorize]
public class DeliveryController : ModuleCrudControllerBase<DeliveryEntity>
{
    public DeliveryController(IRepository<DeliveryEntity, Guid> repo, ITenantContext tenant) : base(repo, tenant) { }
}
