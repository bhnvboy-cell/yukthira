namespace YuktiraERP.Core.Dtos;

public class MicCreateRequest
{
    public string PlantId { get; set; } = "";
    public string CharacteristicCode { get; set; } = "";
    public bool IsQuantitative { get; set; }
    public string ShortText { get; set; } = "";
    public bool LowerSpecLimit { get; set; } = false;
    public bool UpperSpecLimit { get; set; } = false;
    public bool TargetValueRequired { get; set; } = false;
    public string ResultsConfirmation { get; set; } = "SingleResult";
    public string Requirement { get; set; } = "RequiredCharc";
    public int DecimalPlaces { get; set; } = 2;
    public decimal? LowerTolerance { get; set; } = null;
    public decimal? UpperTolerance { get; set; } = null;
    public decimal? TargetValue { get; set; } = null;
}

public class MicSearchFilter
{
    public string? PlantId { get; set; } = null;
    public string? Status { get; set; } = null;
    public string? CharacteristicCode { get; set; } = null;
    public bool? IsQuantitative { get; set; } = null;
}

public class MicDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PlantId { get; set; } = "";
    public string CharacteristicCode { get; set; } = "";
    public DateTime ValidFrom { get; set; }
    public bool IsQuantitative { get; set; }
    public string ShortText { get; set; } = "";
    public string Status { get; set; } = "";
    public bool LowerSpecLimit { get; set; }
    public bool UpperSpecLimit { get; set; }
    public bool TargetValueRequired { get; set; }
    public string ResultsConfirmation { get; set; } = "";
    public string Requirement { get; set; } = "";
    public int DecimalPlaces { get; set; }
    public decimal? LowerTolerance { get; set; }
    public decimal? UpperTolerance { get; set; }
    public decimal? TargetValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
