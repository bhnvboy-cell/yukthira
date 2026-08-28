using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class RFSessionStartRequest
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string StationId { get; set; } = string.Empty;
    }

    public class RFSessionStartResult
    {
        public bool Success { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }

    public class RFSessionEndRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RFSessionEndResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TasksCompleted { get; set; }
    }

    public class RFMenuRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string? MenuPath { get; set; }
    }

    public class RFMenuResult
    {
        public List<RFMenuItem> Items { get; set; } = new();
        public string CurrentPath { get; set; } = string.Empty;
        public string Breadcrumb { get; set; } = string.Empty;
    }

    public class RFMenuItem
    {
        public string MenuItemId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsAvailable { get; set; }
        public string? Tooltip { get; set; }
    }

    public class RFScanValidateRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string ScannedCode { get; set; } = string.Empty;
        public string ScanType { get; set; } = string.Empty;
        public string? ExpectedValue { get; set; }
    }

    public class RFScanValidateResult
    {
        public bool IsValid { get; set; }
        public string CodeType { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
        public string? WarehouseId { get; set; }
        public string? StorageLocation { get; set; }
        public string? Material { get; set; }
        public string? MaterialDescription { get; set; }
        public decimal? Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? Batch { get; set; }
        public string? HandlingUnit { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    public class RFPickPostRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string SourceStorageLocation { get; set; } = string.Empty;
        public string SourceBin { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? SerialNumber { get; set; }
        public string DestinationBin { get; set; } = string.Empty;
    }

    public class RFPickPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal PickedQuantity { get; set; }
        public string? HandlingUnit { get; set; }
    }

    public class RFPutawayPostRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string DestinationStorageLocation { get; set; } = string.Empty;
        public string DestinationBin { get; set; } = string.Empty;
        public string? HandlingUnit { get; set; }
    }

    public class RFPutawayPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PutawayConfirmation { get; set; } = string.Empty;
    }

    public class RFTransferPostRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string SourceStorageLocation { get; set; } = string.Empty;
        public string SourceBin { get; set; } = string.Empty;
        public string DestinationStorageLocation { get; set; } = string.Empty;
        public string DestinationBin { get; set; } = string.Empty;
    }

    public class RFTransferPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransferOrder { get; set; } = string.Empty;
    }

    public class RFCycleCountPostRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string CycleCountId { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public decimal CountedQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? CountComment { get; set; }
    }

    public class RFCycleCountPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal SystemQuantity { get; set; }
        public decimal Variance { get; set; }
        public string VarianceReason { get; set; } = string.Empty;
    }

    public class RFGetOpenTasksRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string? TaskType { get; set; }
        public string? Priority { get; set; }
    }

    public class RFGetOpenTasksResult
    {
        public List<RFOpenTask> Tasks { get; set; } = new();
        public int TotalOpenCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class RFOpenTask
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Material { get; set; }
        public string? MaterialDescription { get; set; }
        public string? SourceBin { get; set; }
        public string? DestinationBin { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class RFGetTaskDetailsRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
    }

    public class RFGetTaskDetailsResult
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string SourceStorageLocation { get; set; } = string.Empty;
        public string SourceBin { get; set; } = string.Empty;
        public string DestinationStorageLocation { get; set; } = string.Empty;
        public string DestinationBin { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? HandlingUnit { get; set; }
        public string? WaveId { get; set; }
        public string? DeliveryNumber { get; set; }
        public string? SalesOrder { get; set; }
        public List<RFScanInstruction> ScanInstructions { get; set; } = new();
    }

    public class RFScanInstruction
    {
        public string InstructionType { get; set; } = string.Empty;
        public string ExpectedCode { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
    }

    public class RFGetSessionSummaryRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class RFGetSessionSummaryResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int TasksStarted { get; set; }
        public int TasksCompleted { get; set; }
        public int UnitsPicked { get; set; }
        public int UnitsPutaway { get; set; }
        public int TransfersCompleted { get; set; }
        public int CycleCountsCompleted { get; set; }
        public decimal TotalDistance { get; set; }
        public int ScanCount { get; set; }
        public int ErrorCount { get; set; }
    }

    public class RFGetWarehouseMapRequest
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string? StorageLocation { get; set; }
        public string? Zone { get; set; }
        public bool IncludeCapacity { get; set; } = false;
        public bool IncludeTaskAssignment { get; set; } = false;
    }

    public class RFGetWarehouseMapResult
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public List<WarehouseZone> Zones { get; set; } = new();
        public int TotalBins { get; set; }
        public int OccupiedBins { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class WarehouseZone
    {
        public string ZoneId { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string ZoneType { get; set; } = string.Empty;
        public List<WarehouseBin> Bins { get; set; } = new();
        public decimal CapacityUtilization { get; set; }
    }

    public class WarehouseBin
    {
        public string BinId { get; set; } = string.Empty;
        public string BinType { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public bool IsAvailable { get; set; }
        public decimal CapacityUsed { get; set; }
        public decimal CapacityTotal { get; set; }
        public string? AssignedTaskId { get; set; }
    }

    public interface IRFWarehouseService
    {
        Task<RFSessionStartResult> StartSessionAsync(RFSessionStartRequest request);
        Task<RFSessionEndResult> EndSessionAsync(RFSessionEndRequest request);
        Task<RFMenuResult> GetMenuAsync(RFMenuRequest request);
        Task<RFScanValidateResult> ValidateScanAsync(RFScanValidateRequest request);
        Task<RFPickPostResult> PostPickAsync(RFPickPostRequest request);
        Task<RFPutawayPostResult> PostPutawayAsync(RFPutawayPostRequest request);
        Task<RFTransferPostResult> PostTransferAsync(RFTransferPostRequest request);
        Task<RFCycleCountPostResult> PostCycleCountAsync(RFCycleCountPostRequest request);
        Task<RFGetOpenTasksResult> GetOpenTasksAsync(RFGetOpenTasksRequest request);
        Task<RFGetTaskDetailsResult> GetTaskDetailsAsync(RFGetTaskDetailsRequest request);
        Task<RFGetSessionSummaryResult> GetSessionSummaryAsync(RFGetSessionSummaryRequest request);
        Task<RFGetWarehouseMapResult> GetWarehouseMapAsync(RFGetWarehouseMapRequest request);
    }
}
