namespace YuktiraERP.Core.Interfaces;

public class MrpSuggestionDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal OpenPoQty { get; set; }
    public decimal TotalDemand { get; set; }
    public decimal ShortageQty { get; set; }
    public string SuggestionType { get; set; } = string.Empty;
    public decimal SuggestedQty { get; set; }
}

public class MrpExplosionResult
{
    public string ProductName { get; set; } = "";
    public decimal PlanQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<MrpRequirementDto> Requirements { get; set; } = new();
    public int BomLevel { get; set; }
}

public class MrpRequirementDto
{
    public int Level { get; set; }
    public string ParentProduct { get; set; } = "";
    public string ComponentCode { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public decimal QtyPerParent { get; set; }
    public decimal GrossRequirement { get; set; }
    public decimal OnHandStock { get; set; }
    public decimal ScheduledReceipts { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal NetRequirement { get; set; }
    public string OrderType { get; set; } = ""; // PURCHASE or PRODUCE
    public string UOM { get; set; } = "EA";
    public DateTime NeedByDate { get; set; }
}

public class MrpRunRequest
{
    public Guid? ProductionPlanId { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MrpRunHistoryDto { public Guid Id { get; set; } public DateTime RunAt { get; set; } public string Status { get; set; } = ""; public int MaterialsProcessed { get; set; } public int SuggestionsGenerated { get; set; } public long DurationMs { get; set; } public List<MrpExceptionMessageDto>? Exceptions { get; set; } }
public class MrpExceptionMessageDto { public Guid Id { get; set; } public string MaterialCode { get; set; } = ""; public string MaterialName { get; set; } = ""; public string ExceptionType { get; set; } = ""; public string Message { get; set; } = ""; public string Severity { get; set; } = ""; public string SuggestedAction { get; set; } = ""; public DateTime CreatedAt { get; set; } }
public class MrpCapacityPlanDto { public string WorkCenterCode { get; set; } = ""; public string WorkCenterName { get; set; } = ""; public decimal AvailableHours { get; set; } public decimal RequiredHours { get; set; } public decimal LoadPercent { get; set; } public string LevelingSuggestion { get; set; } = ""; public List<OperationLoadDetailDto> Operations { get; set; } = new(); }

public interface IMrpService
{
    Task<List<MrpSuggestionDto>> RunMrpAsync(Guid tenantId, Guid? materialId = null);
    Task RefreshStockViewAsync();
    Task<List<MrpSuggestionDto>> GetShortageAlertsAsync(Guid tenantId);
    Task<List<MrpExplosionResult>> ExplodeBomAsync(MrpRunRequest request);
    Task<List<MrpRequirementDto>> CalculateNetRequirementsAsync(Guid tenantId, string productName, decimal quantity, DateTime needByDate);
    Task<List<MrpRequirementDto>> MultiLevelExplosionAsync(Guid tenantId, string productName, decimal quantity, int maxLevel = 10);
    Task<List<MrpRunHistoryDto>> GetRunHistoryAsync(Guid tenantId, int limit = 20);
    Task<List<MrpExceptionMessageDto>> GetExceptionMessagesAsync(Guid tenantId, Guid? runHistoryId = null);
    Task<List<MrpSuggestionDto>> RunMrpMultiPlantAsync(Guid tenantId, Guid? plantId = null);
    Task<List<MrpCapacityPlanDto>> CalculateCapacityLevelingAsync(Guid tenantId, DateTime start, DateTime end);
    Task<List<MrpSuggestionDto>> RunMrpWithVendorLeadTimeAsync(Guid tenantId, Guid? plantId = null);
}
