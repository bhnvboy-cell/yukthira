namespace YuktiraERP.Core.Interfaces;

public class EmissionsOptions
{
    public const string SectionName = "Emissions";

    public bool Enabled { get; set; } = true;
    public decimal MonthlyEnergyKwh { get; set; }
    public decimal DefaultFreightDistanceKm { get; set; } = 100m;
}

public class EmissionFactorDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int Scope { get; set; }
    public string SourceType { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string Unit { get; set; } = "kg";
    public decimal KgCo2ePerUnit { get; set; }
    public string Region { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}

public class EmissionLogDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int Scope { get; set; }
    public string SourceType { get; set; } = "";
    public string ReferenceType { get; set; } = "";
    public string ReferenceId { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public decimal KgCo2e { get; set; }
    public decimal KgCo2ePerUnit { get; set; }
    public string Period { get; set; } = "";
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

public class EmissionsSourceTypeDto
{
    public int Scope { get; set; }
    public string SourceType { get; set; } = "";
    public decimal Kg { get; set; }
}

public class EmissionsSummaryDto
{
    public string Period { get; set; } = "";
    public decimal Scope1Kg { get; set; }
    public decimal Scope2Kg { get; set; }
    public decimal Scope3Kg { get; set; }
    public decimal TotalKg { get; set; }
    public decimal PerUnitKg { get; set; }
    public decimal OutputQuantity { get; set; }
    public string Unit { get; set; } = "kg";
    public List<EmissionsSourceTypeDto> BySourceType { get; set; } = new();
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

public class EmissionsRecomputeResult
{
    public bool Enabled { get; set; } = true;
    public int Inserted { get; set; }
    public int SkippedSources { get; set; }
    public List<string> Skipped { get; set; } = new();
    public int Scope1Count { get; set; }
    public int Scope2Count { get; set; }
    public int Scope3Count { get; set; }
    public List<string> Periods { get; set; } = new();
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

public class EmissionLedgerResult
{
    public List<EmissionLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public interface IEmissionsTrackerService
{
    Task<EmissionsSummaryDto> GetSummaryAsync(Guid tenantId, string? period = null, CancellationToken ct = default);

    Task<EmissionsRecomputeResult> RecomputeAsync(Guid tenantId, string userId, CancellationToken ct);

    Task<List<EmissionFactorDto>> GetFactorsAsync(Guid tenantId, int? scope, string? sourceType, CancellationToken ct = default);

    Task<EmissionFactorDto> UpsertFactorAsync(EmissionFactorDto factor, Guid tenantId, string userId, CancellationToken ct);

    Task<EmissionLedgerResult> GetLedgerAsync(Guid tenantId, string? period, int? scope, int page, int pageSize, CancellationToken ct = default);
}
