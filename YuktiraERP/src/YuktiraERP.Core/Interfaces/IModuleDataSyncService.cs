using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IModuleDataSyncService
{
    Task<ModuleEntityMetaDto> GetEntityMetadataAsync(DataSyncModule module);
    Task<List<ModuleListItemDto>> GetAvailableModulesAsync();
    Task<byte[]> GenerateTemplateAsync(DataSyncModule module, Guid tenantId);
    Task<ImportValidationResultDto> ValidateUploadAsync(DataSyncModule module, List<Dictionary<string, string>> rows, Guid tenantId);
    Task<DataSyncExecuteResultDto> ExecuteSyncAsync(DataSyncModule module, List<Dictionary<string, string>> rows, bool skipErrors, Guid tenantId, Guid userId);
}

public interface ISyncSessionStore
{
    Task<ModuleSyncSessionDto?> GetSessionAsync(string sessionToken);
    Task<string> CreateSessionAsync(ModuleSyncSessionDto session);
    Task RemoveSessionAsync(string sessionToken);
}
