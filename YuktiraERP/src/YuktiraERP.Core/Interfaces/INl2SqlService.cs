namespace YuktiraERP.Core.Interfaces;

public class Nl2SqlOptions
{
    public const string SectionName = "Nl2Sql";

    public int MaxRows { get; set; } = 200;
    public bool AllowDrafting { get; set; } = true;
}

public class Nl2SqlQueryRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public string Text { get; set; } = "";
    public int? Limit { get; set; }
}

public class Nl2SqlQueryResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Intent { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Sql { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public long ExecutedMs { get; set; }
}

public class Nl2SqlDraftRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public string Text { get; set; } = "";
}

public class Nl2SqlDraftResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Condition { get; set; } = "";
    public decimal Value { get; set; }
    public decimal Threshold { get; set; }
    public bool Drafted { get; set; }
    public string? NonConformanceId { get; set; }
}

public interface INl2SqlService
{
    Task<Nl2SqlQueryResult> QueryAsync(Nl2SqlQueryRequest request);
    Task<Nl2SqlDraftResult> DraftAsync(Nl2SqlDraftRequest request);
    List<string> GetSupportedIntents();
}
