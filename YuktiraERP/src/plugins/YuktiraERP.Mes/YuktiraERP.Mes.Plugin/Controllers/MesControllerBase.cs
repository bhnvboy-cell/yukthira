using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YuktiraERP.Mes.Plugin.Controllers;

[ApiController]
[Authorize]
public abstract class MesControllerBase : ControllerBase
{
    protected Guid CurrentTenantId =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    protected string CurrentPlantCode =>
        Request.Headers["X-Plant-Code"].FirstOrDefault() ?? "PLANT01";

    protected string CurrentActor =>
        User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "system";
}
