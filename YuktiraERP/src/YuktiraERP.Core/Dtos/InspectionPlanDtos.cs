namespace YuktiraERP.Core.Dtos;

public class InspectionPlanCreateRequest
{
    public string? PlantId { get; set; }
    public string? MaterialId { get; set; }
    public string Usage { get; set; } = "5";
    public string? Description { get; set; }
    public decimal LotSizeFrom { get; set; } = 0;
    public decimal LotSizeTo { get; set; } = 999999;
}

public class InspectionPlanOperationRequest
{
    public string OperationNo { get; set; } = "";
    public string OperationDescription { get; set; } = "";
    public string? WorkCenter { get; set; }
    public decimal BaseQuantity { get; set; } = 1;
}

public class InspectionPlanMicAssignRequest
{
    public Guid OperationId { get; set; }
    public int CharacteristicNo { get; set; }
    public string MicCode { get; set; } = "";
    public string MicPlantId { get; set; } = "";
    public bool IsQuantitative { get; set; }
    public string ShortText { get; set; } = "";
    public string? InspectionMethodCode { get; set; }
    public string? SamplingProcedureCode { get; set; }
}

public class InspectionPlanResolveRequest
{
    public string? PlantId { get; set; }
    public string? MaterialId { get; set; }
    public string Usage { get; set; } = "5";
}

public class InspectionPlanDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string GroupKey { get; set; } = "";
    public int GroupCounter { get; set; } = 1;
    public string? PlantId { get; set; }
    public string? MaterialId { get; set; }
    public string Usage { get; set; } = "5";
    public string OverallStatus { get; set; } = "4";
    public decimal LotSizeFrom { get; set; }
    public decimal LotSizeTo { get; set; }
    public string? Description { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InspectionPlanOperationDto> Operations { get; set; } = new();
}

public class InspectionPlanOperationDto
{
    public Guid Id { get; set; }
    public string OperationNo { get; set; } = "";
    public string OperationDescription { get; set; } = "";
    public string? WorkCenter { get; set; }
    public decimal BaseQuantity { get; set; } = 1;
    public List<InspectionPlanMicDto> Characteristics { get; set; } = new();
}

public class InspectionPlanMicDto
{
    public Guid Id { get; set; }
    public int CharacteristicNo { get; set; }
    public string MicCode { get; set; } = "";
    public string MicPlantId { get; set; } = "";
    public int MicVersion { get; set; } = 1;
    public bool IsQuantitative { get; set; }
    public string ShortText { get; set; } = "";
    public string? InspectionMethodCode { get; set; }
    public int? InspectionMethodVersion { get; set; }
    public string? SamplingProcedureCode { get; set; }
}

public class AutoGenerationRequest
{
    public string PlantId { get; set; } = "";
    public string MaterialId { get; set; } = "";
    public string? ProductCategory { get; set; }
}
