using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IMdgService
{
    Task<MdgChangeRequestDto> SubmitChangeRequestAsync(MdgSubmitRequest request, Guid tenantId);
    Task<MdgChangeRequestDto> ApproveChangeRequestAsync(Guid requestId, MdgApprovalRequest approval, Guid tenantId);
    Task<MdgChangeRequestDto> RejectChangeRequestAsync(Guid requestId, MdgRejectionRequest rejection, Guid tenantId);
    Task<MdgChangeRequestDto> ActivateChangeRequestAsync(Guid requestId, Guid tenantId);
    Task<List<MdgChangeRequestDto>> GetPendingRequestsAsync(Guid tenantId);
    Task<MdgChangeRequestDto?> GetChangeRequestAsync(Guid requestId, Guid tenantId);
    Task<MdgValidationResult> ValidateStagingPayloadAsync(string entityName, string payload, Guid tenantId);
}
