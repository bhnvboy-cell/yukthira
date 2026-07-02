namespace YuktiraERP.Core.Interfaces;

public interface INumberRangeService
{
    Task<string> GetNextNumberAsync(Guid tenantId, string module, string prefix, int? year = null);
    Task<long> GetCurrentNumberAsync(Guid tenantId, string module, string prefix);
    Task ResetNumberAsync(Guid tenantId, string module, string prefix, long nextNumber);
}
