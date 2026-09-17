using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IModuleTemplateService
{
    Task<byte[]> GenerateEnhancedTemplateAsync(DataSyncModule module, Guid tenantId);
    Task<ModuleEntityMetaDto> GetEntityMetadataWithRelationsAsync(DataSyncModule module, Guid tenantId);
}
