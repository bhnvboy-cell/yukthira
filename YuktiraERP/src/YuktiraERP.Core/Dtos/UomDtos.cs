namespace YuktiraERP.Core.Dtos;

public class UomConversionRequest
{
    public decimal SourceValue { get; set; }
    public string SourceUomCode { get; set; } = "";
    public string TargetUomCode { get; set; } = "";
    public string? MaterialCode { get; set; } = null;
}

public class UomConversionResult
{
    public bool Success { get; set; }
    public decimal SourceValue { get; set; }
    public string SourceUomCode { get; set; } = "";
    public decimal TargetValue { get; set; }
    public string TargetUomCode { get; set; } = "";
    public decimal FactorUsed { get; set; }
    public string Message { get; set; } = "";
}

public class UomCreateUpdateRequest
{
    public string Msehi { get; set; } = "";
    public string IsoCode { get; set; } = "";
    public string DimensionCode { get; set; } = "";
    public decimal Numerator { get; set; } = 1;
    public decimal Denominator { get; set; } = 1;
    public decimal AddOffset { get; set; } = 0;
    public int Decimals { get; set; } = 2;
    public string ShortText { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class UomValidationResult
{
    public bool IsValid { get; set; }
    public decimal RoundedValue { get; set; }
    public string Message { get; set; } = "";
}

public class MaterialUomConversionDto
{
    public Guid Id { get; set; }
    public string MaterialCode { get; set; } = "";
    public string SourceUomCode { get; set; } = "";
    public string TargetUomCode { get; set; } = "";
    public decimal ConversionFactor { get; set; }
    public decimal? DensityFactor { get; set; }
    public string? PlantCode { get; set; }
    public bool IsActive { get; set; } = true;
}
