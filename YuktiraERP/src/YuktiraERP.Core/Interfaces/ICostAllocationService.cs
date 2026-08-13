namespace YuktiraERP.Core.Interfaces;

public class CostAllocationRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string CostElementCode { get; set; } = "";
    public string AllocationType { get; set; } = "Proportional";
    public string Basis { get; set; } = "Headcount";
    public bool IsActive { get; set; } = true;
}

public class CostAllocationBasisDto
{
    public string CostCenterCode { get; set; } = "";
    public string CostCenterName { get; set; } = "";
    public decimal BasisValue { get; set; }
}

public class CostAllocationRunRequest
{
    public string Period { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string CostElementCode { get; set; } = "";
    public string Basis { get; set; } = "Headcount";
    public List<CostAllocationBasisDto> BasisValues { get; set; } = new();
}

public class CostAllocationDetailDto
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string CostCenterCode { get; set; } = "";
    public string CostCenterName { get; set; } = "";
    public string CostElementCode { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
    public string Basis { get; set; } = "";
}

public class CostAllocationRunDto
{
    public Guid Id { get; set; }
    public string Period { get; set; } = "";
    public decimal TotalAllocated { get; set; }
    public string Status { get; set; } = "";
    public DateTime RunAt { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class CostCenterUtilizationDto
{
    public string CostCenterCode { get; set; } = "";
    public string CostCenterName { get; set; } = "";
    public decimal PlannedBudget { get; set; }
    public decimal Allocated { get; set; }
    public decimal UtilizationPercent { get; set; }
}

public interface ICostAllocationService
{
    Task<List<CostAllocationRuleDto>> GetRulesAsync(Guid tenantId);
    Task<CostAllocationRuleDto> CreateRuleAsync(Guid tenantId, CostAllocationRuleDto request);
    Task<CostAllocationRuleDto?> UpdateRuleAsync(Guid tenantId, Guid id, CostAllocationRuleDto request);
    Task DeleteRuleAsync(Guid tenantId, Guid id);

    Task<CostAllocationRunDto> RunAllocationAsync(Guid tenantId, CostAllocationRunRequest request, string createdBy);
    Task<List<CostAllocationRunDto>> GetRunsAsync(Guid tenantId, int limit = 50);
    Task<List<CostAllocationDetailDto>> GetRunDetailsAsync(Guid tenantId, Guid runId);
    Task<List<CostCenterUtilizationDto>> GetUtilizationAsync(Guid tenantId, Guid runId);
}