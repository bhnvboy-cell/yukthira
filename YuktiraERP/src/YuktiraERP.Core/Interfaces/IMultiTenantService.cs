namespace YuktiraERP.Core.Interfaces;

public interface IMultiTenantService
{
    Guid? GetCurrentTenantId();
    string GetCurrentTenantCode();
    string ResolveTenantFromSubdomain();
    string ResolveTenantFromUrl();
}

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantCode { get; }
    string TenantName { get; }
}
