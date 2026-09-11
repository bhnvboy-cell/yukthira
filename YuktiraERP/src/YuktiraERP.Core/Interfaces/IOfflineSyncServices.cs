using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IOfflineQueueService
{
    Task<OfflineQueueProcessResult> ProcessOfflineQueueAsync(IEnumerable<OfflineTransactionDto> queue, Guid tenantId);
    Task<OfflineConflictResult> ResolveConflictAsync(OfflineConflictDto conflict, Guid tenantId);
    Task<List<OfflineTransactionDto>> GetPendingTransactionsAsync(Guid tenantId, string deviceId);
}

public interface IMobileNotificationService
{
    Task SendLowStockAlertAsync(LowStockNotificationDto notification, Guid tenantId);
    Task SendProductionHoldAlertAsync(ProductionHoldNotificationDto notification, Guid tenantId);
    Task SendQualityNonConformanceAlertAsync(QualityAlertDto notification, Guid tenantId);
    Task SendGenericAlertAsync(GenericAlertDto notification, Guid tenantId);
}
