using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class VelocityClassCalculationRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public DateTime AnalysisPeriodFrom { get; set; }
        public DateTime AnalysisPeriodTo { get; set; }
        public int NumberOfClasses { get; set; } = 5;
        public string MovementMetric { get; set; } = "MovementQuantity";
        public bool IncludeSDEmptyMaterials { get; set; } = false;
    }

    public class VelocityClassCalculationResult
    {
        public bool Success { get; set; }
        public int MaterialsProcessed { get; set; }
        public List<VelocityClassDefinition> VelocityClasses { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VelocityClassDefinition
    {
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public decimal MinMovement { get; set; }
        public decimal MaxMovement { get; set; }
        public int MaterialCount { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class SlottingRecommendationRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public string? MaterialNumber { get; set; }
        public string? VelocityClass { get; set; }
        public int? MaxRecommendations { get; set; }
        public bool IncludeCurrentAssignment { get; set; } = true;
        public string OptimizationGoal { get; set; } = "PickEfficiency";
    }

    public class SlottingRecommendationResult
    {
        public List<SlottingRecommendation> Recommendations { get; set; } = new();
        public int TotalCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SlottingRecommendation
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string CurrentBin { get; set; } = string.Empty;
        public string RecommendedBin { get; set; } = string.Empty;
        public string RecommendedZone { get; set; } = string.Empty;
        public string CurrentVelocityClass { get; set; } = string.Empty;
        public string RecommendedVelocityClass { get; set; } = string.Empty;
        public decimal CurrentUtilization { get; set; }
        public decimal RecommendedUtilization { get; set; }
        public int MoveCount { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string ReasonDescription { get; set; } = string.Empty;
        public decimal EstimatedTimeSaving { get; set; }
        public decimal PriorityScore { get; set; }
    }

    public class SlottingApplyRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public List<SlottingChangeItem> Changes { get; set; } = new();
        public bool ValidateOnly { get; set; } = false;
        public string EffectiveDate { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class SlottingChangeItem
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string SourceBin { get; set; } = string.Empty;
        public string DestinationBin { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? Batch { get; set; }
    }

    public class SlottingApplyResult
    {
        public bool Success { get; set; }
        public int ChangesApplied { get; set; }
        public int ChangesValidated { get; set; }
        public List<SlottingChangeError> Errors { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class SlottingChangeError
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class SlottingBatchApplyRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public List<SlottingBatchJob> Jobs { get; set; } = new();
        public bool ValidateOnly { get; set; } = false;
    }

    public class SlottingBatchJob
    {
        public string JobName { get; set; } = string.Empty;
        public string? MaterialFilter { get; set; }
        public string? ZoneFilter { get; set; }
        public string? VelocityClassFilter { get; set; }
        public List<SlottingChangeItem> ManualChanges { get; set; } = new();
    }

    public class SlottingBatchApplyResult
    {
        public bool AllSucceeded { get; set; }
        public int TotalJobs { get; set; }
        public int SuccessJobs { get; set; }
        public int FailedJobs { get; set; }
        public List<SlottingBatchJobResult> JobResults { get; set; } = new();
    }

    public class SlottingBatchJobResult
    {
        public string JobName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int ChangesApplied { get; set; }
        public List<SlottingChangeError> Errors { get; set; } = new();
    }

    public class MaterialSlotRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public string MaterialNumber { get; set; } = string.Empty;
    }

    public class MaterialSlotResult
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string CurrentBin { get; set; } = string.Empty;
        public string CurrentZone { get; set; } = string.Empty;
        public string VelocityClass { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal BinCapacity { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public DateTime LastMovementDate { get; set; }
        public int DaysSinceLastMovement { get; set; }
        public string RecommendedBin { get; set; } = string.Empty;
        public string RecommendedZone { get; set; } = string.Empty;
        public bool NeedsReassignment { get; set; }
    }

    public class SlotUtilizationRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public string? Zone { get; set; }
        public string? BinType { get; set; }
    }

    public class SlotUtilizationResult
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public List<ZoneUtilization> Zones { get; set; } = new();
        public int TotalBins { get; set; }
        public int OccupiedBins { get; set; }
        public int EmptyBins { get; set; }
        public decimal OverallUtilization { get; set; }
    }

    public class ZoneUtilization
    {
        public string ZoneId { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public int TotalBins { get; set; }
        public int OccupiedBins { get; set; }
        public int EmptyBins { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal AverageFillLevel { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SlottingOptimizationRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public string OptimizationStrategy { get; set; } = string.Empty;
        public int MaxMovesPerRun { get; set; } = 100;
        public bool ApplyImmediately { get; set; } = false;
        public DateTime? EffectiveDate { get; set; }
        public List<string>? VelocityClassFilter { get; set; }
        public List<string>? ZoneFilter { get; set; }
    }

    public class SlottingOptimizationResult
    {
        public bool Success { get; set; }
        public int MovesRecommended { get; set; }
        public int MovesApplied { get; set; }
        public decimal EstimatedTimeSavingPerDay { get; set; }
        public decimal EstimatedDistanceSavingPerDay { get; set; }
        public List<SlottingOptimizationMove> Moves { get; set; } = new();
        public DateTime CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SlottingOptimizationMove
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string FromBin { get; set; } = string.Empty;
        public string ToBin { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ImpactScore { get; set; }
        public string Justification { get; set; } = string.Empty;
    }

    public interface IVelocitySlottingService
    {
        Task<VelocityClassCalculationResult> CalculateVelocityClassesAsync(VelocityClassCalculationRequest request);
        Task<SlottingRecommendationResult> GetRecommendationsAsync(SlottingRecommendationRequest request);
        Task<SlottingApplyResult> ApplySlottingAsync(SlottingApplyRequest request);
        Task<SlottingBatchApplyResult> BatchApplySlottingAsync(SlottingBatchApplyRequest request);
        Task<MaterialSlotResult> GetMaterialSlotAsync(MaterialSlotRequest request);
        Task<SlotUtilizationResult> GetSlotUtilizationAsync(SlotUtilizationRequest request);
        Task<SlottingOptimizationResult> RunSlottingOptimizationAsync(SlottingOptimizationRequest request);
    }
}
