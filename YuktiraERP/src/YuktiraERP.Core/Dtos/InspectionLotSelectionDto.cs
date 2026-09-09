namespace YuktiraERP.Core.Dtos;

public enum UsageDecisionFilter
{
    AllLots = 0,
    WithoutUsageDecision = 1,
    WithUsageDecision = 2
}

public class InspectionLotSelectionFilterDto
{
    public DateTime? LotCreatedFrom { get; set; }
    public DateTime? LotCreatedTo { get; set; }
    public DateTime? InspStartDateFrom { get; set; }
    public DateTime? InspStartDateTo { get; set; }
    public DateTime? EndOfInspFrom { get; set; }
    public DateTime? EndOfInspTo { get; set; }

    public string? PlantFrom { get; set; }
    public string? PlantTo { get; set; }
    public string? InspLotOrigin { get; set; }
    public string? MaterialCodeFrom { get; set; }
    public string? MaterialCodeTo { get; set; }
    public string? BatchNumber { get; set; }
    public string? VendorCode { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? CustomerCode { get; set; }
    public string? MaterialClass { get; set; }
    public string? AssignedInspector { get; set; }

    public UsageDecisionFilter UsageDecisionFilter { get; set; } = UsageDecisionFilter.AllLots;
    public string? Layout { get; set; }
    public string? RefFieldMonitor { get; set; }
    public int MaxHits { get; set; } = 100;
}

public class InspectionLotSelectionResultDto
{
    public Guid Id { get; set; }
    public string LotNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string InspectionType { get; set; } = "";
    public string InspectionLotOrigin { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string BaseUOM { get; set; } = "";
    public string Status { get; set; } = "";
    public string AssignedInspector { get; set; } = "";
    public string InspectionPlanID { get; set; } = "";
    public string ReferenceOrderNumber { get; set; } = "";
    public int SampleSize { get; set; }
    public int Inspected { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? VendorCode { get; set; }
    public string? CustomerCode { get; set; }
    public string? ManufacturerCode { get; set; }
    public bool HasUsageDecision { get; set; }
    public string? UDCode { get; set; }
    public string? UDDecision { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string StorageLocation { get; set; } = "";
}

public class InspectionLotSelectionResponseDto
{
    public List<InspectionLotSelectionResultDto> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int ReturnedCount { get; set; }
    public int MaxHits { get; set; }
    public bool HasMore { get; set; }
}
