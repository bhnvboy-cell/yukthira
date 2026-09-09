using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IQualityManagementService
{
    Task<InspectionLotSelectionResponseDto> GetSelectedInspectionLotsAsync(
        InspectionLotSelectionFilterDto filter, Guid tenantId);
}
