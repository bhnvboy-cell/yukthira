using Microsoft.AspNetCore.Http;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.MultiTenant;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items["TenantId"] is Guid tid)
                return tid;
            if (context?.User?.FindFirst("TenantId")?.Value is string s && Guid.TryParse(s, out var tid2))
                return tid2;
            return Guid.Empty;
        }
    }

    public string TenantCode => _httpContextAccessor.HttpContext?.Items["TenantCode"] as string ?? string.Empty;
    public string TenantName => _httpContextAccessor.HttpContext?.Items["TenantName"] as string ?? string.Empty;
}
