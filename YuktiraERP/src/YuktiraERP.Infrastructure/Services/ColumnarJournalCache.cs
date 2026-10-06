using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Serialization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class ColumnarJournalCache : IColumnarJournalCache
{
    private static readonly HashSet<string> AllowedDimensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Period", "FiscalYear", "AccountCode", "AccountType", "Plant",
        "MaterialCode", "VendorCode", "CustomerCode", "DocumentNumber"
    };

    private static readonly HashSet<string> AllowedMeasures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sumDebit", "sumCredit", "sumAmountLC", "count"
    };

    private static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "eq", "ne", "gte", "lte", "between", "in", "contains"
    };

    private sealed class Snapshot
    {
        public Guid[] TenantIds = Array.Empty<Guid>();
        public int[] FiscalYears = Array.Empty<int>();
        public int[] Periods = Array.Empty<int>();
        public string[] AccountCodes = Array.Empty<string>();
        public string[] AccountTypes = Array.Empty<string>();
        public string[] Plants = Array.Empty<string>();
        public string[] MaterialCodes = Array.Empty<string>();
        public string[] VendorCodes = Array.Empty<string>();
        public string[] CustomerCodes = Array.Empty<string>();
        public string[] DocumentNumbers = Array.Empty<string>();
        public decimal[] Debits = Array.Empty<decimal>();
        public decimal[] Credits = Array.Empty<decimal>();
        public decimal[] AmountsLc = Array.Empty<decimal>();
        public DateTime[] PostingDates = Array.Empty<DateTime>();
        public int Count;
    }

    private sealed class Aggregate
    {
        public decimal Debit;
        public decimal Credit;
        public decimal Amount;
        public int Count;
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ColumnarCacheOptions _options;
    private readonly ILogger<ColumnarJournalCache>? _logger;
    private readonly SemaphoreSlim _buildGate = new(1, 1);
    private readonly SemaphoreSlim _exportGate = new(1, 1);
    private Snapshot? _snapshot;
    private int _rowCount;
    private long _lastBuiltTicks;
    private DateTime? _lastExportAt;
    private string _lastExportError = "";

    public ColumnarJournalCache(
        IServiceScopeFactory scopeFactory,
        IOptions<ColumnarCacheOptions> options,
        ILogger<ColumnarJournalCache>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? new ColumnarCacheOptions();
        _logger = logger;
    }

    public int RowCount => Volatile.Read(ref _rowCount);

    public DateTime? LastBuiltAt
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastBuiltTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public async Task<int> BuildAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        await _buildGate.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();

            var query = db.UniversalJournals.AsNoTracking();
            if (tenantId.HasValue) query = query.Where(j => j.TenantId == tenantId.Value);

            var rows = await query.ToListAsync(ct);
            var snapshot = new Snapshot
            {
                Count = rows.Count,
                TenantIds = new Guid[rows.Count],
                FiscalYears = new int[rows.Count],
                Periods = new int[rows.Count],
                AccountCodes = new string[rows.Count],
                AccountTypes = new string[rows.Count],
                Plants = new string[rows.Count],
                MaterialCodes = new string[rows.Count],
                VendorCodes = new string[rows.Count],
                CustomerCodes = new string[rows.Count],
                DocumentNumbers = new string[rows.Count],
                Debits = new decimal[rows.Count],
                Credits = new decimal[rows.Count],
                AmountsLc = new decimal[rows.Count],
                PostingDates = new DateTime[rows.Count]
            };

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                snapshot.TenantIds[i] = row.TenantId;
                snapshot.FiscalYears[i] = row.FiscalYear;
                snapshot.Periods[i] = row.Period;
                snapshot.AccountCodes[i] = row.AccountCode ?? "";
                snapshot.AccountTypes[i] = row.AccountType ?? "";
                snapshot.Plants[i] = row.Plant ?? "";
                snapshot.MaterialCodes[i] = row.MaterialCode ?? "";
                snapshot.VendorCodes[i] = row.VendorCode ?? "";
                snapshot.CustomerCodes[i] = row.CustomerCode ?? "";
                snapshot.DocumentNumbers[i] = row.DocumentNumber ?? "";
                snapshot.Debits[i] = row.DebitAmount;
                snapshot.Credits[i] = row.CreditAmount;
                snapshot.AmountsLc[i] = row.AmountLC;
                snapshot.PostingDates[i] = row.PostingDate;
            }

            Volatile.Write(ref _snapshot, snapshot);
            Volatile.Write(ref _rowCount, snapshot.Count);
            Interlocked.Exchange(ref _lastBuiltTicks, DateTime.UtcNow.Ticks);
            _logger?.LogDebug("Columnar journal cache built with {Rows} rows", snapshot.Count);
            return snapshot.Count;
        }
        finally
        {
            _buildGate.Release();
        }
    }

    public Task<int> RefreshAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        return BuildAsync(tenantId, ct);
    }

    public async Task<ColumnarQueryResult> QueryAsync(ColumnarQuery query, CancellationToken ct = default)
    {
        query ??= new ColumnarQuery();
        ValidateQuery(query);

        var stopwatch = Stopwatch.StartNew();
        var snapshot = Volatile.Read(ref _snapshot);
        if (snapshot == null)
        {
            await BuildAsync(null, ct);
            snapshot = Volatile.Read(ref _snapshot) ?? new Snapshot();
        }

        var matched = new List<int>(snapshot.Count);
        for (var i = 0; i < snapshot.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (query.TenantId.HasValue && snapshot.TenantIds[i] != query.TenantId.Value) continue;
            if (query.From.HasValue && snapshot.PostingDates[i] < query.From.Value) continue;
            if (query.To.HasValue && snapshot.PostingDates[i] > query.To.Value) continue;
            if (!MatchesFilters(snapshot, i, query.Filters)) continue;
            matched.Add(i);
        }

        var groupBy = query.GroupBy.Select(CanonicalDimension).ToList();
        var measures = query.Measures.Count > 0
            ? query.Measures.Select(CanonicalMeasure).ToList()
            : new List<string> { "sumDebit", "sumCredit", "sumAmountLC", "count" };

        var buckets = new Dictionary<string, Aggregate>(StringComparer.Ordinal);
        foreach (var i in matched)
        {
            var key = groupBy.Count == 0
                ? ""
                : string.Join('\u001f', groupBy.Select(group => ReadDimension(snapshot, group, i)));
            if (!buckets.TryGetValue(key, out var aggregate))
            {
                aggregate = new Aggregate();
                buckets[key] = aggregate;
            }

            aggregate.Debit += snapshot.Debits[i];
            aggregate.Credit += snapshot.Credits[i];
            aggregate.Amount += snapshot.AmountsLc[i];
            aggregate.Count++;
        }

        var columns = new List<string>();
        columns.AddRange(groupBy);
        columns.AddRange(measures);

        var limit = query.Limit <= 0 ? 1000 : Math.Min(query.Limit, 10000);
        var rows = new List<Dictionary<string, object?>>();
        foreach (var pair in buckets.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (rows.Count >= limit) break;

            var row = new Dictionary<string, object?>();
            if (groupBy.Count > 0)
            {
                var parts = pair.Key.Split('\u001f');
                for (var i = 0; i < groupBy.Count; i++)
                {
                    row[groupBy[i]] = parts[i];
                }
            }

            foreach (var measure in measures)
            {
                row[measure] = measure switch
                {
                    "sumDebit" => (object)pair.Value.Debit,
                    "sumCredit" => (object)pair.Value.Credit,
                    "sumAmountLC" => (object)pair.Value.Amount,
                    "count" => pair.Value.Count,
                    _ => 0m
                };
            }

            rows.Add(row);
        }

        return new ColumnarQueryResult
        {
            Engine = "ColumnarCache",
            Columns = columns,
            Rows = rows,
            MatchedRows = matched.Count,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<string> ExportAsync(int? fiscalYear = null, CancellationToken ct = default)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        if (snapshot == null)
        {
            await BuildAsync(null, ct);
            snapshot = Volatile.Read(ref _snapshot) ?? new Snapshot();
        }

        var year = fiscalYear ?? DateTime.UtcNow.Year;
        var directory = ResolveDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"uj_{year}.parquet");

        var rows = new List<JournalParquetRow>(snapshot.Count);
        for (var i = 0; i < snapshot.Count; i++)
        {
            if (snapshot.FiscalYears[i] != year) continue;
            rows.Add(new JournalParquetRow
            {
                FiscalYear = snapshot.FiscalYears[i],
                Period = snapshot.Periods[i],
                AccountCode = snapshot.AccountCodes[i],
                AccountType = snapshot.AccountTypes[i],
                Plant = snapshot.Plants[i],
                MaterialCode = snapshot.MaterialCodes[i],
                VendorCode = snapshot.VendorCodes[i],
                CustomerCode = snapshot.CustomerCodes[i],
                DocumentNumber = snapshot.DocumentNumbers[i],
                Dr = snapshot.Debits[i],
                Cr = snapshot.Credits[i],
                AmountLC = snapshot.AmountsLc[i],
                PostingDate = snapshot.PostingDates[i]
            });
        }

        await _exportGate.WaitAsync(ct);
        try
        {
            if (rows.Count == 0)
            {
                _lastExportError = $"No journal rows to export for fiscal year {year}";
                _logger?.LogWarning("Columnar export skipped for fiscal year {FiscalYear}: {Reason}", year, _lastExportError);
                return path;
            }

            await ParquetSerializer.SerializeAsync(rows, path);
            _lastExportAt = DateTime.UtcNow;
            _lastExportError = "";
            _logger?.LogInformation("Columnar export wrote {Rows} rows to {Path}", rows.Count, path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _lastExportError = ex.Message;
            _logger?.LogError(ex, "Columnar export to {Path} failed", path);
            throw;
        }
        finally
        {
            _exportGate.Release();
        }

        return path;
    }

    public async Task<ParquetExportStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var status = new ParquetExportStatus
        {
            Enabled = _options.Enabled,
            LastExportAt = _lastExportAt,
            LastError = _lastExportError,
            TotalRows = RowCount,
            Files = new List<ParquetExportFileInfo>()
        };

        var directory = ResolveDirectory();
        if (!Directory.Exists(directory)) return status;

        foreach (var file in Directory.GetFiles(directory, "uj_*.parquet"))
        {
            ct.ThrowIfCancellationRequested();
            var info = new ParquetExportFileInfo
            {
                Path = file,
                WrittenAt = File.GetLastWriteTimeUtc(file)
            };

            var name = Path.GetFileNameWithoutExtension(file);
            var suffix = name.Substring(name.LastIndexOf('_') + 1);
            info.FiscalYear = int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                ? year
                : 0;
            info.Rows = await CountRowsAsync(file, ct);
            status.Files.Add(info);
        }

        status.Files = status.Files.OrderBy(f => f.FiscalYear).ThenBy(f => f.Path, StringComparer.Ordinal).ToList();
        return status;
    }

    private static async Task<int> CountRowsAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var reader = await ParquetReader.CreateAsync(path);
            long total = 0;
            for (var i = 0; i < reader.RowGroupCount; i++)
            {
                using var group = reader.OpenRowGroupReader(i);
                total += group.RowCount;
            }
            return (int)total;
        }
        catch
        {
            return 0;
        }
    }

    private string ResolveDirectory()
    {
        var directory = string.IsNullOrWhiteSpace(_options.ExportDirectory)
            ? "data/parquet"
            : _options.ExportDirectory;
        return Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(AppContext.BaseDirectory, directory);
    }

    private static void ValidateQuery(ColumnarQuery query)
    {
        foreach (var group in query.GroupBy)
        {
            if (!AllowedDimensions.Contains(group ?? ""))
                throw new ArgumentException($"Unsupported group by column: {group}");
        }

        foreach (var measure in query.Measures)
        {
            if (!AllowedMeasures.Contains(measure ?? ""))
                throw new ArgumentException($"Unsupported measure: {measure}");
        }

        foreach (var filter in query.Filters)
        {
            if (filter == null) continue;

            var field = filter.Field ?? "";
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Filter field is required");
            if (field != "PostingDate" && !AllowedDimensions.Contains(field))
                throw new ArgumentException($"Unsupported filter field: {field}");

            var op = string.IsNullOrWhiteSpace(filter.Op) ? "eq" : filter.Op.ToLowerInvariant();
            if (!AllowedOperators.Contains(op))
                throw new ArgumentException($"Unsupported filter operator: {filter.Op}");

            if (op == "between")
            {
                if (string.IsNullOrEmpty(filter.Value) || string.IsNullOrEmpty(filter.ValueTo))
                    throw new ArgumentException($"Filter {field}: between requires value and valueTo");
            }
            else if (op == "in")
            {
                if (filter.Values == null || filter.Values.Count == 0)
                    throw new ArgumentException($"Filter {field}: in requires values");
            }
            else if (string.IsNullOrEmpty(filter.Value))
            {
                throw new ArgumentException($"Filter {field}: {op} requires value");
            }
        }
    }

    private static bool MatchesFilters(Snapshot snapshot, int index, List<ColumnarFilter> filters)
    {
        foreach (var filter in filters)
        {
            if (filter == null) continue;

            var field = filter.Field ?? "";
            var op = string.IsNullOrWhiteSpace(filter.Op) ? "eq" : filter.Op.ToLowerInvariant();

            if (field == "PostingDate")
            {
                if (op == "in")
                {
                    var posting = snapshot.PostingDates[index];
                    var found = false;
                    foreach (var value in filter.Values ?? new List<string>())
                    {
                        if (TryParseDate(value, out var candidate) && posting.Date == candidate.Date)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                    continue;
                }

                if (!TryParseDate(filter.Value, out var from)) continue;
                var current = snapshot.PostingDates[index];
                if (op == "between")
                {
                    if (!TryParseDate(filter.ValueTo, out var to)) continue;
                    if (current < from || current > to) return false;
                }
                else if (op == "gte" && current < from)
                {
                    return false;
                }
                else if (op == "lte" && current > from)
                {
                    return false;
                }
                else if (op == "eq" && current.Date != from.Date)
                {
                    return false;
                }
                else if (op == "ne" && current.Date == from.Date)
                {
                    return false;
                }

                continue;
            }

            var text = ReadDimension(snapshot, field, index);
            if (IsNumericDimension(field))
            {
                if (!MatchesNumeric(text, filter, op)) return false;
            }
            else if (!MatchesText(text, filter, op))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesText(string text, ColumnarFilter filter, string op)
    {
        switch (op)
        {
            case "eq":
                return string.Equals(text, filter.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case "ne":
                return !string.Equals(text, filter.Value ?? "", StringComparison.OrdinalIgnoreCase);
            case "gte":
                return string.CompareOrdinal(text, filter.Value ?? "") >= 0;
            case "lte":
                return string.CompareOrdinal(text, filter.Value ?? "") <= 0;
            case "between":
                return string.CompareOrdinal(text, filter.Value ?? "") >= 0
                    && string.CompareOrdinal(text, filter.ValueTo ?? "") <= 0;
            case "in":
                return filter.Values != null
                    && filter.Values.Contains(text, StringComparer.OrdinalIgnoreCase);
            case "contains":
                return text.Contains(filter.Value ?? "", StringComparison.OrdinalIgnoreCase);
            default:
                throw new ArgumentException($"Unsupported filter operator: {op}");
        }
    }

    private static bool MatchesNumeric(string text, ColumnarFilter filter, string op)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current))
            return false;

        if (op == "in")
        {
            foreach (var value in filter.Values ?? new List<string>())
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var candidate)
                    && current == candidate)
                {
                    return true;
                }
            }
            return false;
        }

        if (!int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value2))
            throw new ArgumentException($"Filter {filter.Field}: value is not a valid number");

        switch (op)
        {
            case "eq": return current == value2;
            case "ne": return current != value2;
            case "gte": return current >= value2;
            case "lte": return current <= value2;
            case "between":
                if (!int.TryParse(filter.ValueTo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var upper))
                    throw new ArgumentException($"Filter {filter.Field}: valueTo is not a valid number");
                return current >= value2 && current <= upper;
            case "contains": return text.Contains(filter.Value ?? "", StringComparison.Ordinal);
            default: throw new ArgumentException($"Unsupported filter operator: {op}");
        }
    }

    private static bool TryParseDate(string? value, out DateTime parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }

    private static bool IsNumericDimension(string field)
    {
        return string.Equals(field, "Period", StringComparison.OrdinalIgnoreCase)
            || string.Equals(field, "FiscalYear", StringComparison.OrdinalIgnoreCase);
    }

    private static string CanonicalDimension(string field)
    {
        foreach (var dimension in AllowedDimensions)
        {
            if (string.Equals(dimension, field, StringComparison.OrdinalIgnoreCase)) return dimension;
        }
        throw new ArgumentException($"Unsupported columnar column: {field}");
    }

    private static string CanonicalMeasure(string measure)
    {
        foreach (var candidate in AllowedMeasures)
        {
            if (string.Equals(candidate, measure, StringComparison.OrdinalIgnoreCase)) return candidate;
        }
        throw new ArgumentException($"Unsupported measure: {measure}");
    }

    private static string ReadDimension(Snapshot snapshot, string field, int index)
    {
        return CanonicalDimension(field) switch
        {
            "Period" => snapshot.Periods[index].ToString(CultureInfo.InvariantCulture),
            "FiscalYear" => snapshot.FiscalYears[index].ToString(CultureInfo.InvariantCulture),
            "AccountCode" => snapshot.AccountCodes[index],
            "AccountType" => snapshot.AccountTypes[index],
            "Plant" => snapshot.Plants[index],
            "MaterialCode" => snapshot.MaterialCodes[index],
            "VendorCode" => snapshot.VendorCodes[index],
            "CustomerCode" => snapshot.CustomerCodes[index],
            "DocumentNumber" => snapshot.DocumentNumbers[index],
            _ => ""
        };
    }
}
