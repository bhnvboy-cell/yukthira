namespace YuktiraERP.Core.Interfaces;

public class DdmrpBufferZoneDto
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Plant { get; set; } = "1000";
    public decimal AverageDailyUsage { get; set; } // ADU
    public int DecoupledLeadTimeDays { get; set; } // DLT
    public decimal MinimumOrderQuantity { get; set; } // MOQ
    public decimal LeadTimeFactor { get; set; } = 0.5m; // 0.2 to 0.8
    public decimal VariabilityFactor { get; set; } = 0.4m; // 0.2 to 0.7

    // Decoupled Buffer Zones
    public decimal RedZone { get; set; }     // Safety buffer
    public decimal YellowZone { get; set; }  // Primary coverage
    public decimal GreenZone { get; set; }   // Order cycle buffer

    public decimal TopOfRed => RedZone;
    public decimal TopOfYellow => RedZone + YellowZone;
    public decimal TopOfGreen => RedZone + YellowZone + GreenZone;

    // Current State
    public decimal OnHandStock { get; set; }
    public decimal OpenSupplyQuantity { get; set; } // Inbound POs + Prod Orders
    public decimal QualifiedDemandSpikes { get; set; } // Immediate high sales orders
    public decimal NetFlowPosition => OnHandStock + OpenSupplyQuantity - QualifiedDemandSpikes;

    public string BufferStatus { get; set; } = "Healthy-Green"; // "Critical-Red", "Reorder-Yellow", "Healthy-Green", "Overstock-Blue"
    public decimal RecommendedOrderQuantity { get; set; }
}

public interface IDdmrpBufferService
{
    Task<DdmrpBufferZoneDto> CalculateBufferZonesAsync(Guid tenantId, string materialCode, string plant);
    Task<List<DdmrpBufferZoneDto>> EvaluateAllBuffersAsync(Guid tenantId, string plant);
    Task<List<DdmrpBufferZoneDto>> GetReplenishmentProposalsAsync(Guid tenantId, string plant);
}
