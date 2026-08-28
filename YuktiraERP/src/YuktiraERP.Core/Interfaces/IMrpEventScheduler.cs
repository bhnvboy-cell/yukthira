using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class MrpEventPublishRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string EventSource { get; set; } = string.Empty;
        public string? PlantId { get; set; }
        public string? MaterialNumber { get; set; }
        public string? OrderNumber { get; set; }
        public string? BomId { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
        public string Priority { get; set; } = "Normal";
        public DateTime? ScheduledTime { get; set; }
    }

    public class MrpEventPublishResult
    {
        public bool Success { get; set; }
        public string EventId { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public int SubscribersNotified { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MrpEventSubscribeRequest
    {
        public string SubscriberId { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public List<string> EventTypes { get; set; } = new();
        public string? PlantFilter { get; set; }
        public string? MaterialFilter { get; set; }
        public string CallbackUrl { get; set; } = string.Empty;
        public string? WebhookSecret { get; set; }
    }

    public class MrpEventSubscribeResult
    {
        public bool Success { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime SubscribedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MrpEventUnsubscribeRequest
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string SubscriberId { get; set; } = string.Empty;
    }

    public class MrpEventUnsubscribeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EventDrivenMrpRunRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string RunType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string>? MaterialNumbers { get; set; }
        public string? MrpType { get; set; }
        public bool ProcessDependentRequirements { get; set; } = true;
        public bool ConsiderSafetyStock { get; set; } = true;
        public bool ConsiderReorderPoint { get; set; } = true;
    }

    public class EventDrivenMrpRunResult
    {
        public bool Success { get; set; }
        public string RunId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int MaterialsProcessed { get; set; }
        public int PlannedOrdersCreated { get; set; }
        public int PurchaseRequisitionsCreated { get; set; }
        public int DependentRequirementsProcessed { get; set; }
        public int ExceptionsGenerated { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class NetChangeMrpRunRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public DateTime SinceDate { get; set; }
        public string? MrpArea { get; set; }
        public List<string>? MaterialNumbers { get; set; }
        public bool IncludeDeletedElements { get; set; } = false;
        public bool ProcessExceptionMessages { get; set; } = true;
    }

    public class NetChangeMrpRunResult
    {
        public bool Success { get; set; }
        public string RunId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ChangedMaterialsProcessed { get; set; }
        public int NewPlannedOrders { get; set; }
        public int ChangedPlannedOrders { get; set; }
        public int DeletedPlannedOrders { get; set; }
        public int NewPurchaseRequisitions { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class FullMrpRunRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public DateTime PlanningHorizonStart { get; set; }
        public DateTime PlanningHorizonEnd { get; set; }
        public string? MrpGroup { get; set; }
        public string? MrpController { get; set; }
        public List<string>? MaterialNumbers { get; set; }
        public bool CreatePurchaseRequisitions { get; set; } = true;
        public bool CreatePlannedOrders { get; set; } = true;
        public bool ConsiderRescheduling { get; set; } = true;
    }

    public class FullMrpRunResult
    {
        public bool Success { get; set; }
        public string RunId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalMaterialsProcessed { get; set; }
        public int PlannedOrdersCreated { get; set; }
        public int PlannedOrdersChanged { get; set; }
        public int PlannedOrdersDeleted { get; set; }
        public int PurchaseRequisitionsCreated { get; set; }
        public int DependentRequirementsCreated { get; set; }
        public int ReschedulingRecommendations { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class EventStreamRequest
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public string? PlantId { get; set; }
        public string? MaterialNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageSize { get; set; } = 50;
        public string? ContinuationToken { get; set; }
    }

    public class EventStreamResult
    {
        public List<MrpEvent> Events { get; set; } = new();
        public string? NextContinuationToken { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class MrpEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventSource { get; set; } = string.Empty;
        public string? PlantId { get; set; }
        public string? MaterialNumber { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Payload { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    public class MaterialBalanceRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string MaterialNumber { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? MrpArea { get; set; }
        public bool IncludeSubComponents { get; set; } = false;
    }

    public class MaterialBalanceResult
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string PlantId { get; set; } = string.Empty;
        public decimal OpeningStock { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalIssues { get; set; }
        public decimal ClosingStock { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal AvailableStock { get; set; }
        public List<MaterialBalanceLine> BalanceLines { get; set; } = new();
        public List<MaterialBalanceException> Exceptions { get; set; } = new();
    }

    public class MaterialBalanceLine
    {
        public DateTime Date { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal RunningTotal { get; set; }
        public string? Remarks { get; set; }
    }

    public class MaterialBalanceException
    {
        public string ExceptionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ExceptionDate { get; set; }
        public decimal ShortageQuantity { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public class PlanningRunHistoryRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string? RunType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PlanningRunHistoryResult
    {
        public List<PlanningRunHistoryItem> Runs { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class PlanningRunHistoryItem
    {
        public string RunId { get; set; } = string.Empty;
        public string RunType { get; set; } = string.Empty;
        public string PlantId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MaterialsProcessed { get; set; }
        public int PlannedOrdersCreated { get; set; }
        public int PurchaseRequisitionsCreated { get; set; }
        public decimal DurationSeconds { get; set; }
        public string? TriggerSource { get; set; }
    }

    public class PendingEventsRequest
    {
        public string? PlantId { get; set; }
        public string? EventType { get; set; }
        public string? Priority { get; set; }
        public DateTime? ScheduledBefore { get; set; }
        public int MaxEvents { get; set; } = 100;
    }

    public class PendingEventsResult
    {
        public List<MrpEvent> PendingEvents { get; set; } = new();
        public int TotalPending { get; set; }
        public int HighPriorityCount { get; set; }
        public int NormalPriorityCount { get; set; }
        public int LowPriorityCount { get; set; }
    }

    public class EventReplayRequest
    {
        public string EventId { get; set; } = string.Empty;
        public bool OverwriteExisting { get; set; } = false;
        public bool NotifySubscribers { get; set; } = true;
    }

    public class EventReplayResult
    {
        public bool Success { get; set; }
        public string EventId { get; set; } = string.Empty;
        public DateTime ReplayedAt { get; set; }
        public int SubscribersNotified { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EventStatisticsRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? GroupBy { get; set; }
    }

    public class EventStatisticsResult
    {
        public string PlantId { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public int ProcessedEvents { get; set; }
        public int FailedEvents { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageProcessingTime { get; set; }
        public List<EventStatisticsByType> ByType { get; set; } = new();
        public List<EventStatisticsByDate> ByDate { get; set; } = new();
        public List<EventStatisticsTopSource> TopSources { get; set; } = new();
    }

    public class EventStatisticsByType
    {
        public string EventType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal AverageProcessingTime { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class EventStatisticsByDate
    {
        public DateTime Date { get; set; }
        public int EventCount { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class EventStatisticsTopSource
    {
        public string Source { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public interface IMrpEventScheduler
    {
        Task<MrpEventPublishResult> PublishEventAsync(MrpEventPublishRequest request);
        Task<MrpEventSubscribeResult> SubscribeAsync(MrpEventSubscribeRequest request);
        Task<MrpEventUnsubscribeResult> UnsubscribeAsync(MrpEventUnsubscribeRequest request);
        Task<EventDrivenMrpRunResult> RunEventDrivenMrpAsync(EventDrivenMrpRunRequest request);
        Task<NetChangeMrpRunResult> RunNetChangeMrpAsync(NetChangeMrpRunRequest request);
        Task<FullMrpRunResult> RunFullMrpAsync(FullMrpRunRequest request);
        Task<EventStreamResult> GetEventStreamAsync(EventStreamRequest request);
        Task<MaterialBalanceResult> GetMaterialBalanceAsync(MaterialBalanceRequest request);
        Task<PlanningRunHistoryResult> GetPlanningRunHistoryAsync(PlanningRunHistoryRequest request);
        Task<PendingEventsResult> GetPendingEventsAsync(PendingEventsRequest request);
        Task<EventReplayResult> ReplayEventAsync(EventReplayRequest request);
        Task<EventStatisticsResult> GetEventStatisticsAsync(EventStatisticsRequest request);
    }
}
