using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Interfaces;

public interface IEdiService
{
    Task<string> ConvertToEdifactAsync(object data, string documentType);
    Task<string> ConvertToX12Async(object data, string documentType);
    Task<object> ParseEdifactAsync(string ediContent);
    Task<object> ParseX12Async(string ediContent);

    EdiMessageType? ParseMessageType(string? documentType);
    string GetDocumentTypeLabel(EdiMessageType messageType);

    Task<EdiTransmissionPageResult> GetTransmissionsAsync(Guid tenantId, EdiTransmissionQuery query);
    Task<EdiTransmissionDetail?> GetTransmissionAsync(Guid tenantId, Guid id);
    Task<EdiTransmissionStatsResult> GetTransmissionStatsAsync(Guid tenantId);
    Task LogTransmissionAsync(Guid tenantId, EdiTransmissionLogEntry entry);
}
