using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public partial class NaturalLanguageQueryEngine : INaturalLanguageQueryEngine
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<NaturalLanguageQueryEngine> _logger;

    private static readonly Dictionary<string, NlQueryEntityType> EntityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["material"] = NlQueryEntityType.Material,
        ["materials"] = NlQueryEntityType.Material,
        ["item"] = NlQueryEntityType.Material,
        ["items"] = NlQueryEntityType.Material,
        ["product"] = NlQueryEntityType.Material,
        ["products"] = NlQueryEntityType.Material,
        ["vendor"] = NlQueryEntityType.Vendor,
        ["vendors"] = NlQueryEntityType.Vendor,
        ["supplier"] = NlQueryEntityType.Vendor,
        ["suppliers"] = NlQueryEntityType.Vendor,
        ["customer"] = NlQueryEntityType.Customer,
        ["customers"] = NlQueryEntityType.Customer,
        ["client"] = NlQueryEntityType.Customer,
        ["purchase order"] = NlQueryEntityType.PurchaseOrder,
        ["po"] = NlQueryEntityType.PurchaseOrder,
        ["purchase orders"] = NlQueryEntityType.PurchaseOrder,
        ["sales order"] = NlQueryEntityType.SalesOrder,
        ["so"] = NlQueryEntityType.SalesOrder,
        ["sales orders"] = NlQueryEntityType.SalesOrder,
        ["order"] = NlQueryEntityType.SalesOrder,
        ["orders"] = NlQueryEntityType.SalesOrder,
        ["inspection"] = NlQueryEntityType.InspectionLot,
        ["inspection lot"] = NlQueryEntityType.InspectionLot,
        ["inspection lots"] = NlQueryEntityType.InspectionLot,
        ["stock"] = NlQueryEntityType.StockBalance,
        ["inventory"] = NlQueryEntityType.StockBalance,
        ["balance"] = NlQueryEntityType.StockBalance,
        ["journal"] = NlQueryEntityType.JournalEntry,
        ["journal entry"] = NlQueryEntityType.JournalEntry,
        ["journal entries"] = NlQueryEntityType.JournalEntry,
        ["gl entry"] = NlQueryEntityType.JournalEntry,
        ["employee"] = NlQueryEntityType.Employee,
        ["employees"] = NlQueryEntityType.Employee,
        ["staff"] = NlQueryEntityType.Employee,
        ["production order"] = NlQueryEntityType.ProductionOrder,
        ["production orders"] = NlQueryEntityType.ProductionOrder,
        ["prod order"] = NlQueryEntityType.ProductionOrder,
    };

    private static readonly Dictionary<string, NlQueryOperation> OperationKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["show"] = NlQueryOperation.Select,
        ["list"] = NlQueryOperation.Select,
        ["get"] = NlQueryOperation.Select,
        ["find"] = NlQueryOperation.Select,
        ["display"] = NlQueryOperation.Select,
        ["how many"] = NlQueryOperation.Count,
        ["count"] = NlQueryOperation.Count,
        ["total"] = NlQueryOperation.Sum,
        ["sum"] = NlQueryOperation.Sum,
        ["add up"] = NlQueryOperation.Sum,
        ["average"] = NlQueryOperation.Average,
        ["avg"] = NlQueryOperation.Average,
        ["mean"] = NlQueryOperation.Average,
        ["filter"] = NlQueryOperation.Filter,
        ["where"] = NlQueryOperation.Filter,
        ["with"] = NlQueryOperation.Filter,
        ["group"] = NlQueryOperation.GroupBy,
        ["group by"] = NlQueryOperation.GroupBy,
        ["sort"] = NlQueryOperation.Sort,
        ["order by"] = NlQueryOperation.Sort,
    };

    public NaturalLanguageQueryEngine(YuktiraDbContext db, ILogger<NaturalLanguageQueryEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<NlQueryResult> ExecuteQueryAsync(NlQueryRequest request)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var normalizedQuery = request.Query.Trim().ToLowerInvariant();
            var entity = DetectEntityType(normalizedQuery);
            var operation = DetectOperation(normalizedQuery);
            var filters = ExtractFilters(normalizedQuery);

            var sqlBuilder = new System.Text.StringBuilder();
            var parameters = new List<object>();
            string tableName = GetTableName(entity);

            switch (operation)
            {
                case NlQueryOperation.Count:
                    sqlBuilder.Append($"SELECT COUNT(*) as \"Count\" FROM {tableName}");
                    break;
                case NlQueryOperation.Sum:
                    var sumColumn = GetSumColumn(entity);
                    sqlBuilder.Append($"SELECT SUM({sumColumn}) as \"Total\" FROM {tableName}");
                    break;
                case NlQueryOperation.Average:
                    var avgColumn = GetSumColumn(entity);
                    sqlBuilder.Append($"SELECT AVG({avgColumn}) as \"Average\" FROM {tableName}");
                    break;
                default:
                    sqlBuilder.Append($"SELECT * FROM {tableName}");
                    break;
            }

            var whereClause = BuildWhereClause(filters, entity);
            if (!string.IsNullOrEmpty(whereClause))
                sqlBuilder.Append($" WHERE {whereClause}");

            if (operation == NlQueryOperation.GroupBy)
            {
                var groupColumn = GetGroupColumn(entity);
                sqlBuilder.Clear();
                sqlBuilder.Append($"SELECT {groupColumn}, COUNT(*) as \"Count\" FROM {tableName}");
                if (!string.IsNullOrEmpty(whereClause))
                    sqlBuilder.Append($" WHERE {whereClause}");
                sqlBuilder.Append($" GROUP BY {groupColumn}");
            }

            if (operation == NlQueryOperation.Sort || normalizedQuery.Contains("sort") || normalizedQuery.Contains("order by"))
            {
                var sortColumn = GetSortColumn(entity);
                var sortDir = normalizedQuery.Contains("descending") || normalizedQuery.Contains("desc") ? "DESC" : "ASC";
                sqlBuilder.Append($" ORDER BY {sortColumn} {sortDir}");
            }
            else if (entity == NlQueryEntityType.InspectionLot)
            {
                sqlBuilder.Append(" ORDER BY \"CreatedAt\" DESC");
            }

            sqlBuilder.Append($" LIMIT {request.MaxResults}");

            var sql = sqlBuilder.ToString();
            _logger.LogInformation("NL Query: {Query} -> SQL: {Sql}", request.Query, sql);

            var rows = await ExecuteRawQueryAsync(sql);

            var executionTime = DateTime.UtcNow - startTime;

            return new NlQueryResult
            {
                Success = true,
                TranslatedQuery = sql,
                EntityType = entity.ToString(),
                Operation = operation,
                Rows = rows,
                RowCount = rows.Count,
                ExecutionPlan = request.ExplainPlan ? $"Entity={entity}, Op={operation}, Filters={filters.Count}, Table={tableName}" : null,
                ExecutionTime = executionTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NL Query failed: {Query}", request.Query);
            return new NlQueryResult
            {
                Success = false,
                Errors = { $"Query processing failed: {ex.Message}" },
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    public Task<List<NlQuerySuggestion>> GetSuggestionsAsync(string partialQuery, Guid tenantId)
    {
        var suggestions = new List<NlQuerySuggestion>();
        var lower = partialQuery.ToLowerInvariant();

        var templates = new List<(string Query, string Desc, string Entity)>
        {
            ("Show all overdue invoices for client 1000", "List overdue invoices by client number", "Customer"),
            ("Count inspection lots in quality inspection", "Count lots by inspection status", "InspectionLot"),
            ("List materials below minimum stock", "Find low-stock materials", "StockBalance"),
            ("Get total purchase order value this month", "Sum PO values for current month", "PurchaseOrder"),
            ("Show open non-conformances by severity", "NCR list grouped by severity", "InspectionLot"),
            ("List vendors with pending deliveries", "Vendors awaiting GR", "Vendor"),
            ("Average inspection time last 30 days", "Average time metrics", "InspectionLot"),
            ("Show production orders on hold", "List held production orders", "ProductionOrder"),
            ("Count sales orders by status", "SO distribution by status", "SalesOrder"),
            ("List employees in department HR", "Employee lookup by department", "Employee"),
        };

        foreach (var (query, desc, entity) in templates)
        {
            if (lower.Length < 2 || query.ToLowerInvariant().Contains(lower))
            {
                suggestions.Add(new NlQuerySuggestion
                {
                    Query = query,
                    Description = desc,
                    EntityType = entity
                });
            }
        }

        return Task.FromResult(suggestions);
    }

    private static NlQueryEntityType DetectEntityType(string query)
    {
        foreach (var kv in EntityKeywords.OrderByDescending(k => k.Key.Length))
        {
            if (query.Contains(kv.Key))
                return kv.Value;
        }
        return NlQueryEntityType.Material;
    }

    private static NlQueryOperation DetectOperation(string query)
    {
        foreach (var kv in OperationKeywords.OrderByDescending(k => k.Key.Length))
        {
            if (query.Contains(kv.Key))
                return kv.Value;
        }
        return NlQueryOperation.Select;
    }

    private static List<(string Field, string Value, string Op)> ExtractFilters(string query)
    {
        var filters = new List<(string, string, string)>();

        var clientMatch = ClientNumberRegex().Match(query);
        if (clientMatch.Success)
            filters.Add(("\"ClientCode\"", clientMatch.Groups[1].Value, "="));

        var statusMatch = StatusRegex().Match(query);
        if (statusMatch.Success)
            filters.Add(("\"Status\"", $"'{statusMatch.Groups[1].Value}'", "="));

        var plantMatch = PlantRegex().Match(query);
        if (plantMatch.Success)
            filters.Add(("\"Plant\"", $"'{plantMatch.Groups[1].Value}'", "="));

        var materialMatch = MaterialRegex().Match(query);
        if (materialMatch.Success)
            filters.Add(("\"MaterialCode\"", $"'{materialMatch.Groups[1].Value}'", "="));

        var dateMatch = DateRangeRegex().Match(query);
        if (dateMatch.Success)
        {
            var days = int.Parse(dateMatch.Groups[1].Value);
            var fromDate = DateTime.UtcNow.AddDays(-days);
            filters.Add(("\"CreatedAt\"", $"'{fromDate:yyyy-MM-dd}'", ">="));
        }

        if (query.Contains("overdue") || query.Contains("past due"))
            filters.Add(("\"DueDate\"", $"'{DateTime.UtcNow:yyyy-MM-dd}'", "<"));

        if (query.Contains("today"))
            filters.Add(("\"CreatedAt\"", $"'{DateTime.UtcNow:yyyy-MM-dd}'", ">="));

        if (query.Contains("this week"))
        {
            var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            filters.Add(("\"CreatedAt\"", $"'{weekStart:yyyy-MM-dd}'", ">="));
        }

        if (query.Contains("this month"))
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            filters.Add(("\"CreatedAt\"", $"'{monthStart:yyyy-MM-dd}'", ">="));
        }

        return filters;
    }

    private static string BuildWhereClause(List<(string Field, string Value, string Op)> filters, NlQueryEntityType entity)
    {
        if (!filters.Any()) return string.Empty;
        return string.Join(" AND ", filters.Select(f => $"{f.Field} {f.Op} {f.Value}"));
    }

    private static string GetTableName(NlQueryEntityType entity) => entity switch
    {
        NlQueryEntityType.Material => "yuktira_mm.material_masters",
        NlQueryEntityType.Vendor => "yuktira_mm.vendors",
        NlQueryEntityType.Customer => "yuktira_sd.customers",
        NlQueryEntityType.PurchaseOrder => "yuktira_mm.purchase_orders",
        NlQueryEntityType.SalesOrder => "yuktira_sd.sales_orders",
        NlQueryEntityType.InspectionLot => "yuktira_qm.inspection_lots",
        NlQueryEntityType.StockBalance => "yuktira_mm.stock_balances",
        NlQueryEntityType.JournalEntry => "yuktira_fi.journal_entries",
        NlQueryEntityType.Employee => "yuktira_hr.employees",
        NlQueryEntityType.ProductionOrder => "yuktira_pp.production_orders",
        _ => "yuktira_mm.material_masters"
    };

    private static string GetSumColumn(NlQueryEntityType entity) => entity switch
    {
        NlQueryEntityType.PurchaseOrder => "\"TotalAmount\"",
        NlQueryEntityType.SalesOrder => "\"TotalAmount\"",
        NlQueryEntityType.StockBalance => "\"Quantity\"",
        NlQueryEntityType.JournalEntry => "\"Amount\"",
        _ => "\"Id\""
    };

    private static string GetGroupColumn(NlQueryEntityType entity) => entity switch
    {
        NlQueryEntityType.Material => "\"MaterialGroup\"",
        NlQueryEntityType.InspectionLot => "\"Status\"",
        NlQueryEntityType.PurchaseOrder => "\"Status\"",
        NlQueryEntityType.SalesOrder => "\"Status\"",
        NlQueryEntityType.StockBalance => "\"Plant\"",
        _ => "\"Status\""
    };

    private static string GetSortColumn(NlQueryEntityType entity) => entity switch
    {
        NlQueryEntityType.Material => "\"MaterialCode\"",
        NlQueryEntityType.PurchaseOrder => "\"CreatedAt\"",
        NlQueryEntityType.SalesOrder => "\"CreatedAt\"",
        NlQueryEntityType.InspectionLot => "\"CreatedAt\"",
        NlQueryEntityType.StockBalance => "\"Quantity\"",
        _ => "\"CreatedAt\""
    };

    private async Task<List<Dictionary<string, object?>>> ExecuteRawQueryAsync(string sql)
    {
        var results = new List<Dictionary<string, object?>>();
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }
        return results;
    }

    [GeneratedRegex(@"client\s+(\d+)")]
    private static partial Regex ClientNumberRegex();
    [GeneratedRegex(@"status\s+(\w+)")]
    private static partial Regex StatusRegex();
    [GeneratedRegex(@"plant\s+(\w+)")]
    private static partial Regex PlantRegex();
    [GeneratedRegex(@"material\s+(\w+)")]
    private static partial Regex MaterialRegex();
    [GeneratedRegex(@"last\s+(\d+)\s+days")]
    private static partial Regex DateRangeRegex();
}
