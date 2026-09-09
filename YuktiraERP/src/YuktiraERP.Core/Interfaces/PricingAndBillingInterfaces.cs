using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IPricingEngineService
{
    Task<PricingResult> CalculateItemPricingAsync(PricingContext context, Guid tenantId);
    Task<List<PricingConditionDto>> GetActiveConditionsAsync(Guid tenantId);
    Task<PricingConditionDto> CreateConditionAsync(Guid tenantId, PricingConditionDto condition);
}

public interface ISalesBillingService
{
    Task<BillingReleaseResult> ReleaseBillingDocumentToFIAsync(Guid billingDocumentId, Guid tenantId);
    Task<BillingDocumentDetailDto> GetBillingDocumentDetailAsync(Guid billingDocumentId, Guid tenantId);
}
