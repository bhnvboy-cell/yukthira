namespace YuktiraERP.Core.Interfaces;

public class VisionQmOptions
{
    public const string SectionName = "VisionQm";

    public decimal MaxImpurityPct { get; set; } = 3.0m;
    public string DefaultStorageLocation { get; set; } = "0001";
    public string GateReference { get; set; } = "VISION-GATE";
}

public class VisionQmFrameRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public string ImageBase64 { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "1000";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "EA";
    public string? BatchNumber { get; set; }
    public string? VendorCode { get; set; }
}

public class VisionQmGateResultDto
{
    public bool Passed { get; set; }
    public string Grade { get; set; } = "";
    public decimal ImpurityPct { get; set; }
    public string DefectType { get; set; } = "";
    public string Severity { get; set; } = "";
    public float Confidence { get; set; }
    public string? MaterialDocumentNumber { get; set; }
    public string? InspectionLotNumber { get; set; }
    public string? NonConformanceId { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class InspectionResultRowDto
{
    public string ResultId { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public string Characteristic { get; set; } = "";
    public string Result { get; set; } = "";
    public string Evaluation { get; set; } = "";
    public decimal MeasuredValue { get; set; }
    public string Unit { get; set; } = "";
    public string Status { get; set; } = "";
    public string InspectorNotes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public interface IVisionQmGateService
{
    Task<VisionQmGateResultDto> ProcessFrameAsync(VisionQmFrameRequest request, CancellationToken cancellationToken = default);
    Task<List<InspectionResultRowDto>> GetRecentResultsAsync(Guid tenantId, string? materialCode, int page, int pageSize);
}
