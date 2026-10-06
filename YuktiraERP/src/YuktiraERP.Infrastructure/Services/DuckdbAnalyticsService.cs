using System.Diagnostics;
using System.Globalization;
using DuckDB.NET.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public interface IDuckdbAnalyticsService
{
    Task<ColumnarQueryResult> QueryAsync(ColumnarQuery query, CancellationToken ct = default);
}

public class DuckdbAnalyticsService : IDuckdbAnalyticsService
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

    private readonly IColumnarJournalCache _cache;
    private readonly ColumnarCacheOptions _cacheOptions;
    private readonly ILogger<DuckdbAnalyticsService>? _logger;

    public DuckdbAnalyticsService(
        IColumnarJournalCache cache,
        IOptions<ColumnarCacheOptions> options,
        ILogger<DuckdbAnalyticsService>? logger = null)
    {
        _cache = cache;
        _cacheOptions = options?.Value ?? new ColumnarCacheOptions();
        _logger = logger;
    }

    public async Task<ColumnarQueryResult> QueryAsync(ColumnarQuery query, CancellationToken ct = default)
    {
        query ??= new ColumnarQuery();
        ValidateQuery(query);

        if (query.TenantId.HasValue)
        {
            return await FallbackAsync(query, "Tenant scoped analytics run on the columnar cache; parquet has no tenant column", ct);
        }

        var source = ResolveParquetFile(query);
        if (source == null)
        {
            return await FallbackAsync(query, "No parquet export is available for the requested analytics query", ct);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var sql = BuildSql(query, source);
            var groupColumns = new HashSet<string>(
                query.GroupBy.Select(CanonicalDimension),
                StringComparer.OrdinalIgnoreCase);
            var result = new ColumnarQueryResult
            {
                Engine = "DuckDB",
                Columns = BuildColumns(query)
            };

            using var connection = new DuckDBConnection("Data Source=:memory:");
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[name] = groupColumns.Contains(name)
                        ? ToGroupValue(value)
                        : ToMeasureValue(name, value);
                }
                result.Rows.Add(row);
            }

            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.UsedFallback = false;
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWarning(ex, "DuckDB analytics query failed; falling back to the columnar cache");
            var fallback = await FallbackAsync(query, ex.Message, ct);
            fallback.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return fallback;
        }
    }

    private async Task<ColumnarQueryResult> FallbackAsync(ColumnarQuery query, string reason, CancellationToken ct)
    {
        var result = await _cache.QueryAsync(query, ct);
        result.Engine = "ColumnarCache";
        result.UsedFallback = true;
        result.Error = reason;
        return result;
    }

    private string? ResolveParquetFile(ColumnarQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.File))
        {
            return File.Exists(query.File) ? query.File : null;
        }

        var directories = new List<string>();
        var configured = query.Source;
        if (!string.IsNullOrWhiteSpace(configured) && configured != "cache" && configured != "parquet")
        {
            directories.Add(Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured));
        }

        var exportDirectory = string.IsNullOrWhiteSpace(_cacheOptions.ExportDirectory)
            ? "data/parquet"
            : _cacheOptions.ExportDirectory;
        directories.Add(Path.IsPathRooted(exportDirectory)
            ? exportDirectory
            : Path.Combine(AppContext.BaseDirectory, exportDirectory));
        directories.Add(AppContext.BaseDirectory);

        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory)) continue;
            var files = Directory.GetFiles(directory, "uj_*.parquet");
            if (files.Length > 0) return files.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        return null;
    }

    private static string BuildSql(ColumnarQuery query, string source)
    {
        var groups = query.GroupBy.Select(CanonicalDimension).ToList();
        var measures = query.Measures.Count > 0
            ? query.Measures.Select(CanonicalMeasure).ToList()
            : new List<string> { "sumDebit", "sumCredit", "sumAmountLC", "count" };

        var select = new List<string>();
        select.AddRange(groups);
        select.AddRange(measures.Select(MeasureSql));

        var sql = $"SELECT {string.Join(", ", select)} FROM read_parquet('{Escape(source)}')";

        var where = BuildWhere(query);
        if (where.Count > 0) sql += $" WHERE {string.Join(" AND ", where)}";
        if (groups.Count > 0) sql += $" GROUP BY {string.Join(", ", Enumerable.Range(1, groups.Count))}";
        if (groups.Count > 0) sql += $" ORDER BY {string.Join(", ", Enumerable.Range(1, groups.Count))}";

        var limit = query.Limit <= 0 ? 1000 : Math.Min(query.Limit, 10000);
        sql += $" LIMIT {limit.ToString(CultureInfo.InvariantCulture)}";
        return sql;
    }

    private static List<string> BuildWhere(ColumnarQuery query)
    {
        var clauses = new List<string>();

        if (query.From.HasValue)
            clauses.Add($"CAST(PostingDate AS DATE) >= DATE '{query.From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}'");
        if (query.To.HasValue)
            clauses.Add($"CAST(PostingDate AS DATE) <= DATE '{query.To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}'");

        foreach (var filter in query.Filters)
        {
            if (filter == null) continue;

            var field = filter.Field ?? "";
            var op = string.IsNullOrWhiteSpace(filter.Op) ? "eq" : filter.Op.ToLowerInvariant();

            if (field == "PostingDate")
            {
                clauses.Add(DateClause(op, filter));
                continue;
            }

            var column = CanonicalDimension(field);
            if (IsNumericDimension(column))
            {
                clauses.Add(NumericClause(column, op, filter));
            }
            else
            {
                clauses.Add(TextClause(column, op, filter));
            }
        }

        return clauses;
    }

    private static string DateClause(string op, ColumnarFilter filter)
    {
        return op switch
        {
            "gte" => $"CAST(PostingDate AS DATE) >= {DateLiteral(filter.Value)}",
            "lte" => $"CAST(PostingDate AS DATE) <= {DateLiteral(filter.Value)}",
            "between" => $"CAST(PostingDate AS DATE) BETWEEN {DateLiteral(filter.Value)} AND {DateLiteral(filter.ValueTo)}",
            "in" => $"CAST(PostingDate AS DATE) IN ({string.Join(", ", (filter.Values ?? new List<string>()).Select(DateLiteral))})",
            "ne" => $"CAST(PostingDate AS DATE) <> {DateLiteral(filter.Value)}",
            _ => $"CAST(PostingDate AS DATE) = {DateLiteral(filter.Value)}"
        };
    }

    private static string NumericClause(string column, string op, ColumnarFilter filter)
    {
        return op switch
        {
            "ne" => $"{column} <> {IntegerLiteral(filter.Value, column)}",
            "gte" => $"{column} >= {IntegerLiteral(filter.Value, column)}",
            "lte" => $"{column} <= {IntegerLiteral(filter.Value, column)}",
            "between" => $"{column} BETWEEN {IntegerLiteral(filter.Value, column)} AND {IntegerLiteral(filter.ValueTo, column)}",
            "in" => $"{column} IN ({string.Join(", ", (filter.Values ?? new List<string>()).Select(v => IntegerLiteral(v, column)))})",
            "contains" => $"CAST({column} AS VARCHAR) LIKE '%{Escape(filter.Value ?? "")}%'",
            _ => $"{column} = {IntegerLiteral(filter.Value, column)}"
        };
    }

    private static string TextClause(string column, string op, ColumnarFilter filter)
    {
        var value = StringLiteral(filter.Value);
        return op switch
        {
            "ne" => $"{column} <> {value}",
            "gte" => $"{column} >= {value}",
            "lte" => $"{column} <= {value}",
            "between" => $"{column} BETWEEN {value} AND {StringLiteral(filter.ValueTo)}",
            "in" => $"{column} IN ({string.Join(", ", (filter.Values ?? new List<string>()).Select(StringLiteral))})",
            "contains" => $"{column} LIKE '%{Escape(filter.Value ?? "")}%'",
            _ => $"{column} = {value}"
        };
    }

    private static string MeasureSql(string measure)
    {
        return CanonicalMeasure(measure) switch
        {
            "sumDebit" => "SUM(Dr) AS sumDebit",
            "sumCredit" => "SUM(Cr) AS sumCredit",
            "sumAmountLC" => "SUM(AmountLC) AS sumAmountLC",
            _ => "COUNT(*) AS \"count\""
        };
    }

    private static List<string> BuildColumns(ColumnarQuery query)
    {
        var columns = query.GroupBy.Select(CanonicalDimension).ToList();
        columns.AddRange(query.Measures.Count > 0
            ? query.Measures.Select(CanonicalMeasure)
            : new List<string> { "sumDebit", "sumCredit", "sumAmountLC", "count" });
        return columns;
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

    private static string IntegerLiteral(string? value, string column)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            throw new ArgumentException($"Filter {column}: value is not a valid number");
        return parsed.ToString(CultureInfo.InvariantCulture);
    }

    private static string DateLiteral(string? value)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            throw new ArgumentException($"Filter PostingDate: value is not a valid date");
        return $"DATE '{parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}'";
    }

    private static string StringLiteral(string? value) => $"'{Escape(value ?? "")}'";

    private static string Escape(string value) => value.Replace("'", "''");

    private static object? ToGroupValue(object? value)
    {
        if (value == null || value is DBNull) return null;
        if (value is string text) return text;
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    }

    private static object? ToMeasureValue(string name, object? value)
    {
        if (value == null || value is DBNull) return null;
        if (string.Equals(name, "count", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        if (value is double d) return Convert.ToDecimal(d, CultureInfo.InvariantCulture);
        if (value is float f) return Convert.ToDecimal(f, CultureInfo.InvariantCulture);
        if (value is bool or string or DateTime or decimal) return value;
        if (value is long or int or short or byte or ulong or uint or ushort or sbyte)
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
