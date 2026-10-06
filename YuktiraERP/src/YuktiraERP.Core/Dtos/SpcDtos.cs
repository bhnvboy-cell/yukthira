namespace YuktiraERP.Core.Dtos;

/// <summary>Filter passed to <c>ISpcEngineService.AnalyzeAsync</c>.</summary>
public class SpcQueryDto
{
    public string? MaterialCode { get; set; }
    public string? Plant { get; set; }
    public string? Characteristic { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>Distinct filter values for the SPC dashboard dropdowns.</summary>
public class SpcFilterOptionsDto
{
    public List<string> Characteristics { get; set; } = new();
    public List<string> MaterialCodes { get; set; } = new();
    public List<string> Plants { get; set; } = new();
}

/// <summary>One plotted control-chart point (one subgroup).</summary>
public class SpcPointDto
{
    /// <summary>Zero-based position of the point in the series (also the index used by violations).</summary>
    public int Index { get; set; }
    /// <summary>Subgroup label (inspection lot number).</summary>
    public string Label { get; set; } = "";
    /// <summary>Point value; null for n&lt;2 subgroups on the R and S charts.</summary>
    public double? Value { get; set; }
    public double? Ucl { get; set; }
    public double? Cl { get; set; }
    public double? Lcl { get; set; }
    /// <summary>True when the point lies outside its control limits.</summary>
    public bool Violating { get; set; }
}

/// <summary>A single control chart (X̄, R or S).</summary>
public class SpcChartDto
{
    public string Title { get; set; } = "";
    public string Unit { get; set; } = "";
    public List<SpcPointDto> Points { get; set; } = new();
}

/// <summary>The three control charts of an analysis.</summary>
public class SpcChartsDto
{
    public SpcChartDto Xbar { get; set; } = new();
    public SpcChartDto R { get; set; } = new();
    public SpcChartDto S { get; set; } = new();
}

/// <summary>A subgroup (inspection lot) summary.</summary>
public class SpcSubgroupDto
{
    /// <summary>One-based subgroup number in time order.</summary>
    public int Index { get; set; }
    public string LotNumber { get; set; } = "";
    /// <summary>Chart label (lot number).</summary>
    public string Label { get; set; } = "";
    public int N { get; set; }
    public double Mean { get; set; }
    /// <summary>Range; null when n &lt; 2.</summary>
    public double? Range { get; set; }
    /// <summary>Sample standard deviation; null when n &lt; 2.</summary>
    public double? StdDev { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>A run-rule (Nelson / Western Electric) violation on the X̄ series.</summary>
public class SpcViolationDto
{
    public string RuleId { get; set; } = "";
    public string RuleName { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Zero-based indexes of the offending points.</summary>
    public List<int> PointIndexes { get; set; } = new();
}

/// <summary>Process capability / performance indices.</summary>
public class SpcCapabilityDto
{
    public double? Cp { get; set; }
    public double? Cpk { get; set; }
    public double? Pp { get; set; }
    public double? Ppk { get; set; }
    public double? Lsl { get; set; }
    public double? Usl { get; set; }
    public double? Mu { get; set; }
    public double? SigmaWithin { get; set; }
    public double? SigmaOverall { get; set; }
}

/// <summary>Complete SPC analysis payload returned to the dashboard.</summary>
public class SpcAnalysisDto
{
    public string Characteristic { get; set; } = "";
    public string Unit { get; set; } = "";
    public string? MaterialCode { get; set; }
    public string? Plant { get; set; }
    public int SubgroupCount { get; set; }
    public int ResultCount { get; set; }
    public List<SpcSubgroupDto> Subgroups { get; set; } = new();
    public SpcChartsDto Charts { get; set; } = new();
    public SpcCapabilityDto? Capability { get; set; }
    public List<SpcViolationDto> Violations { get; set; } = new();
    public bool OutOfControl { get; set; }
}
