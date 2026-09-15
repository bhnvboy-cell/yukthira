using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;
public interface IMicService
{
    Task<MicDto> CreateMicAsync(MicCreateRequest request, Guid tenantId);
    Task<List<MicDto>> SearchMicsAsync(MicSearchFilter filter, Guid tenantId);
    Task<bool> CheckMicExistsAsync(string plantId, string characteristicCode, Guid tenantId);
    Task<MicDto?> GetMicByIdAsync(Guid id, Guid tenantId);
}
