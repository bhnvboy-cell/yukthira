namespace YuktiraERP.Core.Interfaces;

public class ColumnarFilter
{
    public string Field { get; set; } = "";
    public string Op { get; set; } = "eq";
    public string? Value { get; set; }
    public string? ValueTo { get; set; }
    public List<string>? Values { get; set; }
}

public class ColumnarQuery
{
    public Guid? TenantId { get; set; }
    public List<string> GroupBy { get; set; } = new();
    public List<string> Measures { get; set; } = new();
    public List<ColumnarFilter> Filters { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Limit { get; set; } = 1000;
    public string Source { get; set; } = "cache";
    public string? File { get; set; }
}

public class ColumnarQueryResult
{
    public string Engine { get; set; } = "ColumnarCache";
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int MatchedRows { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public bool UsedFallback { get; set; }
    public string Error { get; set; } = "";
}

public class JournalParquetRow
{
    public int FiscalYear { get; set; }
    public int Period { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string Plant { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
    public decimal AmountLC { get; set; }
    public DateTime PostingDate { get; set; }
}

public class ParquetExportFileInfo
{
    public string Path { get; set; } = "";
    public int FiscalYear { get; set; }
    public int Rows { get; set; }
    public DateTime WrittenAt { get; set; }
}

public class ParquetExportStatus
{
    public bool Enabled { get; set; }
    public DateTime? LastExportAt { get; set; }
    public string LastError { get; set; } = "";
    public int TotalRows { get; set; }
    public List<ParquetExportFileInfo> Files { get; set; } = new();
}

public class ColumnarCacheOptions
{
    public const string SectionName = "ColumnarCache";

    public bool Enabled { get; set; } = true;
    public int RefreshMinutes { get; set; } = 15;
    public int ExportMinutes { get; set; } = 60;
    public string ExportDirectory { get; set; } = "data/parquet";
}

public interface IColumnarJournalCache
{
    int RowCount { get; }
    DateTime? LastBuiltAt { get; }

    Task<int> BuildAsync(Guid? tenantId = null, CancellationToken ct = default);

    Task<int> RefreshAsync(Guid? tenantId = null, CancellationToken ct = default);

    Task<ColumnarQueryResult> QueryAsync(ColumnarQuery query, CancellationToken ct = default);

    Task<string> ExportAsync(int? fiscalYear = null, CancellationToken ct = default);

    Task<ParquetExportStatus> GetStatusAsync(CancellationToken ct = default);
}
