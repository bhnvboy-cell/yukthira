using YuktiraERP.Core.Dtos;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Core.Interfaces;

public interface IUomConversionService
{
    Task<UomConversionResult> ConvertAsync(UomConversionRequest request, Guid tenantId);
    Task<List<UnitOfMeasureEntity>> GetActiveUomsAsync(Guid tenantId);
    Task<UnitOfMeasureEntity?> GetUomByCodeAsync(string msehi, Guid tenantId);
    Task<UnitOfMeasureEntity> CreateOrUpdateUomAsync(UomCreateUpdateRequest request, Guid tenantId);
    Task<List<MaterialUomConversionDto>> GetMaterialConversionsAsync(string materialCode, Guid tenantId);
    UomValidationResult ValidateDecimalPrecision(string uomCode, decimal value);
}
