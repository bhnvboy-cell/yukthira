using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class ScheduleCreateRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string SchedulingArea { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SchedulingHorizon { get; set; } = string.Empty;
        public bool ConsiderCapacityConstraints { get; set; } = true;
        public bool ConsiderMaterialAvailability { get; set; } = true;
        public List<string>? OrderNumbers { get; set; }
        public List<string>? WorkCenterFilter { get; set; }
    }

    public class ScheduleCreateResult
    {
        public bool Success { get; set; }
        public string ScheduleId { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int OperationsCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ScheduleCalculateRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string SchedulingDirection { get; set; } = string.Empty;
        public bool ForwardSchedule { get; set; } = true;
        public bool OptimizeSchedule { get; set; } = true;
        public int MaxIterations { get; set; } = 100;
        public decimal? ImprovementThreshold { get; set; }
    }

    public class ScheduleCalculateResult
    {
        public bool Success { get; set; }
        public string ScheduleId { get; set; } = string.Empty;
        public int OperationsScheduled { get; set; }
        public int ConflictsDetected { get; set; }
        public decimal ScheduleScore { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<SchedulingWarning> Warnings { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class SchedulingWarning
    {
        public string WarningCode { get; set; } = string.Empty;
        public string WarningType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AffectedOrder { get; set; }
        public string? AffectedWorkCenter { get; set; }
    }

    public class RescheduleOperationRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public string? WorkCenterId { get; set; }
        public bool ConfirmDependencies { get; set; } = true;
        public bool AllowEarlyStart { get; set; } = false;
    }

    public class RescheduleOperationResult
    {
        public bool Success { get; set; }
        public string OperationId { get; set; } = string.Empty;
        public DateTime OldStartDate { get; set; }
        public DateTime OldEndDate { get; set; }
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public int DependentOperationsRescheduled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CapacityLoadRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string? WorkCenterId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CapacityType { get; set; } = "Machine";
        public bool IncludeUtilization { get; set; } = true;
        public bool IncludeBacklog { get; set; } = true;
    }

    public class CapacityLoadResult
    {
        public List<CapacityLoadItem> LoadItems { get; set; } = new();
        public decimal TotalCapacity { get; set; }
        public decimal TotalLoad { get; set; }
        public decimal OverallUtilization { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
    }

    public class CapacityLoadItem
    {
        public string WorkCenterId { get; set; } = string.Empty;
        public string WorkCenterName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal AvailableCapacity { get; set; }
        public decimal PlannedLoad { get; set; }
        public decimal ConfirmedLoad { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string? BottleneckStatus { get; set; }
        public List<CapacityOrderDetail> Orders { get; set; } = new();
    }

    public class CapacityOrderDetail
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string OperationNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal RequiredCapacity { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    public class ScheduleGanttRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public DateTime? ViewStartDate { get; set; }
        public DateTime? ViewEndDate { get; set; }
        public string? WorkCenterFilter { get; set; }
        public string? OrderFilter { get; set; }
        public string ViewMode { get; set; } = "WorkCenter";
    }

    public class ScheduleGanttResult
    {
        public string ScheduleId { get; set; } = string.Empty;
        public DateTime ViewStartDate { get; set; }
        public DateTime ViewEndDate { get; set; }
        public List<GanttResource> Resources { get; set; } = new();
        public List<GanttTask> Tasks { get; set; } = new();
        public List<GanttDependency> Dependencies { get; set; } = new();
        public List<GanttTimeScale> TimeScales { get; set; } = new();
    }

    public class GanttResource
    {
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public decimal TotalCapacity { get; set; }
        public decimal UtilizedCapacity { get; set; }
    }

    public class GanttTask
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string OperationNumber { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class GanttDependency
    {
        public string FromTaskId { get; set; } = string.Empty;
        public string ToTaskId { get; set; } = string.Empty;
        public string DependencyType { get; set; } = string.Empty;
        public bool IsCriticalPath { get; set; }
    }

    public class GanttTimeScale
    {
        public string ScaleType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class CriticalPathRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public List<string>? OrderNumbers { get; set; }
    }

    public class CriticalPathResult
    {
        public string ScheduleId { get; set; } = string.Empty;
        public decimal CriticalPathLength { get; set; }
        public decimal TotalSlack { get; set; }
        public List<CriticalPathSegment> Segments { get; set; } = new();
        public List<string> CriticalOrderNumbers { get; set; } = new();
        public DateTime EarliestCompletionDate { get; set; }
        public DateTime LatestCompletionDate { get; set; }
    }

    public class CriticalPathSegment
    {
        public string OperationId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string OperationNumber { get; set; } = string.Empty;
        public string WorkCenterId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Duration { get; set; }
        public decimal Slack { get; set; }
        public bool IsBottleneck { get; set; }
    }

    public class ScheduleConflictsRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string? ConflictType { get; set; }
        public string? Severity { get; set; }
    }

    public class ScheduleConflictsResult
    {
        public List<ScheduleConflict> Conflicts { get; set; } = new();
        public int TotalConflicts { get; set; }
        public int CriticalConflicts { get; set; }
    }

    public class ScheduleConflict
    {
        public string ConflictId { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AffectedOrder1 { get; set; }
        public string? AffectedOrder2 { get; set; }
        public string? AffectedWorkCenter { get; set; }
        public DateTime ConflictDateTime { get; set; }
        public List<string> ResolutionSuggestions { get; set; } = new();
    }

    public class ConflictResolveRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string ConflictId { get; set; } = string.Empty;
        public string ResolutionStrategy { get; set; } = string.Empty;
        public DateTime? NewStartTime { get; set; }
        public DateTime? NewEndTime { get; set; }
        public string? AlternativeWorkCenter { get; set; }
    }

    public class ConflictResolveResult
    {
        public bool Success { get; set; }
        public string ConflictId { get; set; } = string.Empty;
        public string AppliedStrategy { get; set; } = string.Empty;
        public int OperationsRescheduled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ScheduleOptimizeRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string OptimizationGoal { get; set; } = string.Empty;
        public int MaxIterations { get; set; } = 50;
        public decimal? TargetImprovement { get; set; }
        public bool OptimizeSetUpTime { get; set; } = true;
        public bool OptimizeTransportTime { get; set; } = true;
        public bool OptimizeWaitTime { get; set; } = true;
    }

    public class ScheduleOptimizeResult
    {
        public bool Success { get; set; }
        public decimal OriginalScore { get; set; }
        public decimal OptimizedScore { get; set; }
        public decimal ImprovementPercentage { get; set; }
        public int MovesPerformed { get; set; }
        public DateTime CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MaterialAvailabilityRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public List<string>? OrderNumbers { get; set; }
        public bool IncludeSubComponents { get; set; } = true;
        public bool CheckPlantStock { get; set; } = true;
        public bool CheckSupplierStock { get; set; } = false;
    }

    public class MaterialAvailabilityResult
    {
        public List<MaterialAvailabilityItem> Items { get; set; } = new();
        public int TotalMaterials { get; set; }
        public int AvailableMaterials { get; set; }
        public int ShortageMaterials { get; set; }
        public decimal OverallAvailability { get; set; }
    }

    public class MaterialAvailabilityItem
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public DateTime? ExpectedAvailabilityDate { get; set; }
        public string? AffectedOrder { get; set; }
    }

    public class SimulateRescheduleRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public string? AlternativeWorkCenter { get; set; }
    }

    public class SimulateRescheduleResult
    {
        public bool IsFeasible { get; set; }
        public string OperationId { get; set; } = string.Empty;
        public int DirectlyAffectedOperations { get; set; }
        public int IndirectlyAffectedOperations { get; set; }
        public int NewConflictsCreated { get; set; }
        public List<SimulatedRescheduleImpact> Impacts { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class SimulatedRescheduleImpact
    {
        public string OperationId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OldStartDate { get; set; }
        public DateTime OldEndDate { get; set; }
        public DateTime NewStartDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public decimal DelayDays { get; set; }
    }

    public class ScheduleExportRequest
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string ExportFormat { get; set; } = "JSON";
        public bool IncludeGanttData { get; set; } = true;
        public bool IncludeCapacityData { get; set; } = true;
        public bool IncludeMaterialData { get; set; } = false;
        public DateTime? ExportDateFrom { get; set; }
        public DateTime? ExportDateTo { get; set; }
    }

    public class ScheduleExportResult
    {
        public bool Success { get; set; }
        public string ExportUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; }
    }

    public interface IFiniteCapacityScheduler
    {
        Task<ScheduleCreateResult> CreateScheduleAsync(ScheduleCreateRequest request);
        Task<ScheduleCalculateResult> CalculateScheduleAsync(ScheduleCalculateRequest request);
        Task<RescheduleOperationResult> RescheduleOperationAsync(RescheduleOperationRequest request);
        Task<CapacityLoadResult> GetCapacityLoadAsync(CapacityLoadRequest request);
        Task<ScheduleGanttResult> GetScheduleGanttAsync(ScheduleGanttRequest request);
        Task<CriticalPathResult> IdentifyCriticalPathAsync(CriticalPathRequest request);
        Task<ScheduleConflictsResult> GetConflictsAsync(ScheduleConflictsRequest request);
        Task<ConflictResolveResult> ResolveConflictAsync(ConflictResolveRequest request);
        Task<ScheduleOptimizeResult> OptimizeScheduleAsync(ScheduleOptimizeRequest request);
        Task<MaterialAvailabilityResult> GetMaterialAvailabilityAsync(MaterialAvailabilityRequest request);
        Task<SimulateRescheduleResult> SimulateRescheduleAsync(SimulateRescheduleRequest request);
        Task<ScheduleExportResult> ExportScheduleAsync(ScheduleExportRequest request);
    }
}
