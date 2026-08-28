using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class WaveCreateRequest
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string WaveName { get; set; } = string.Empty;
        public string WaveType { get; set; } = string.Empty;
        public DateTime? PlannedExecutionDate { get; set; }
        public string? Priority { get; set; }
        public string? DeliveryGroup { get; set; }
        public string? ShippingPoint { get; set; }
        public List<string>? DeliveryNumbers { get; set; }
        public List<string>? SalesOrderNumbers { get; set; }
        public List<WaveCreationCriteria>? Criteria { get; set; }
    }

    public class WaveCreationCriteria
    {
        public string CriteriaType { get; set; } = string.Empty;
        public string CriteriaValue { get; set; } = string.Empty;
    }

    public class WaveCreateResult
    {
        public bool Success { get; set; }
        public string WaveId { get; set; } = string.Empty;
        public string WaveNumber { get; set; } = string.Empty;
        public int TotalLines { get; set; }
        public int TotalQuantity { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WaveReleaseRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public bool OptimizeBeforeRelease { get; set; } = false;
        public bool AssignPickersAutomatically { get; set; } = true;
    }

    public class WaveReleaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TasksCreated { get; set; }
        public DateTime ReleasedAt { get; set; }
    }

    public class WaveAssignPickerRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int MaxPickLines { get; set; } = 50;
    }

    public class WaveAssignPickerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AssignedLines { get; set; }
    }

    public class WaveCompletePickLineRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public string PickLineId { get; set; } = string.Empty;
        public string? HandlingUnit { get; set; }
        public decimal PickedQuantity { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime PickedAt { get; set; }
    }

    public class WaveCompletePickLineResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int LinesRemaining { get; set; }
        public bool WaveComplete { get; set; }
    }

    public class WaveShortPickRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public string PickLineId { get; set; } = string.Empty;
        public decimal PickedQuantity { get; set; }
        public string ShortReason { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool CreateBackorder { get; set; } = true;
    }

    public class WaveShortPickResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackorderWaveId { get; set; }
        public string? BackorderNumber { get; set; }
    }

    public class WaveProgressRequest
    {
        public string WaveId { get; set; } = string.Empty;
    }

    public class WaveProgressResult
    {
        public string WaveId { get; set; } = string.Empty;
        public string WaveNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalLines { get; set; }
        public int CompletedLines { get; set; }
        public int InProgressLines { get; set; }
        public int PendingLines { get; set; }
        public int ShortPickedLines { get; set; }
        public decimal CompletionPercentage { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal PickedQuantity { get; set; }
        public List<WavePickerProgress> PickerProgress { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class WavePickerProgress
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int AssignedLines { get; set; }
        public int CompletedLines { get; set; }
        public decimal CompletionPercentage { get; set; }
    }

    public class WaveGetOpenWavesRequest
    {
        public string? WarehouseId { get; set; }
        public string? WaveType { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class WaveGetOpenWavesResult
    {
        public List<WaveHeader> Waves { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class WaveHeader
    {
        public string WaveId { get; set; } = string.Empty;
        public string WaveNumber { get; set; } = string.Empty;
        public string WaveType { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int TotalLines { get; set; }
        public int CompletedLines { get; set; }
        public decimal CompletionPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PlannedExecutionDate { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }

    public class WaveOptimizeRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public string OptimizationStrategy { get; set; } = string.Empty;
        public bool ReassignPickers { get; set; } = true;
        public bool OptimizeRoute { get; set; } = true;
    }

    public class WaveOptimizeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int LinesReorganized { get; set; }
        public int PickersReassigned { get; set; }
        public decimal EstimatedTimeSaving { get; set; }
        public decimal EstimatedDistanceSaving { get; set; }
    }

    public class WaveCancelRequest
    {
        public string WaveId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool CancelInProgressPicks { get; set; } = false;
    }

    public class WaveCancelResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CancelledLines { get; set; }
        public int LinesReturnedToPool { get; set; }
    }

    public interface IWavePickService
    {
        Task<WaveCreateResult> CreateWaveAsync(WaveCreateRequest request);
        Task<WaveReleaseResult> ReleaseWaveAsync(WaveReleaseRequest request);
        Task<WaveAssignPickerResult> AssignWaveToPickerAsync(WaveAssignPickerRequest request);
        Task<WaveCompletePickLineResult> CompletePickLineAsync(WaveCompletePickLineRequest request);
        Task<WaveShortPickResult> ShortPickAsync(WaveShortPickRequest request);
        Task<WaveProgressResult> GetWaveProgressAsync(WaveProgressRequest request);
        Task<WaveGetOpenWavesResult> GetOpenWavesAsync(WaveGetOpenWavesRequest request);
        Task<WaveOptimizeResult> OptimizeWaveAsync(WaveOptimizeRequest request);
        Task<WaveCancelResult> CancelWaveAsync(WaveCancelRequest request);
    }
}
