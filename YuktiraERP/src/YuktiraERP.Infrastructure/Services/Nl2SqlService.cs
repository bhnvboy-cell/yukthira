using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public partial class Nl2SqlService : INl2SqlService
{
    private static readonly string[] DangerousTokens =
    {
        ";", "--", "/*", "*/", "'", "\"",
        " drop ", " delete ", " insert ", " update ", " alter ", " truncate ",
        " union ", " exec ", " execute ", "xp_", "char(", "waitfor", "0x"
    };

    private static readonly Dictionary<string, EntityDescriptor> Entities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["purchaseorders"] = new EntityDescriptor
        {
            Key = "purchaseorders",
            DisplayName = "PurchaseOrders",
            Keywords = new[] { "purchase orders", "purchase order", "po", "purchaseorders" },
            Columns = new[] { "PoNumber", "VendorCode", "VendorName", "ItemName", "Quantity", "Amount", "TotalAmount", "ItemCount", "Status", "Date" },
            MeasureColumn = "Amount",
            GroupKeys = new[] { "Status", "VendorCode" },
            DateColumn = "Date",
            HasTenantId = true
        },
        ["salesorders"] = new EntityDescriptor
        {
            Key = "salesorders",
            DisplayName = "SalesOrders",
            Keywords = new[] { "sales orders", "sales order", "salesorders" },
            Columns = new[] { "OrderNumber", "CustomerName", "OrderDate", "ItemCount", "Amount", "Status" },
            MeasureColumn = "Amount",
            GroupKeys = new[] { "Status", "CustomerName" },
            DateColumn = "OrderDate",
            HasTenantId = false
        },
        ["inspectionlots"] = new EntityDescriptor
        {
            Key = "inspectionlots",
            DisplayName = "InspectionLots",
            Keywords = new[] { "inspection lots", "inspection lot", "inspectionlots" },
            Columns = new[] { "LotNumber", "MaterialCode", "MaterialName", "Plant", "BatchNumber", "InspectionType", "Quantity", "Status", "Inspected", "Passed", "Failed", "CreatedAt" },
            MeasureColumn = "Quantity",
            GroupKeys = new[] { "Status", "MaterialCode", "Plant" },
            DateColumn = "CreatedAt",
            HasTenantId = true
        },
        ["inspectionresults"] = new EntityDescriptor
        {
            Key = "inspectionresults",
            DisplayName = "InspectionResults",
            Keywords = new[] { "inspection results", "inspection result", "inspectionresults" },
            Columns = new[] { "ResultId", "LotNumber", "BatchNumber", "Characteristic", "Result", "Evaluation", "MeasuredValue", "Unit", "Status", "CreatedAt" },
            MeasureColumn = "MeasuredValue",
            GroupKeys = new[] { "Status", "Evaluation", "LotNumber" },
            DateColumn = "CreatedAt",
            HasTenantId = true
        },
        ["universaljournals"] = new EntityDescriptor
        {
            Key = "universaljournals",
            DisplayName = "UniversalJournals",
            Keywords = new[] { "universal journal", "journal entries", "journal entry", "journal lines", "journal", "gl entries", "universaljournals" },
            Columns = new[] { "DocumentNumber", "DocumentType", "PostingDate", "AccountCode", "AccountName", "DebitAmount", "CreditAmount", "Currency", "VendorCode", "MaterialCode", "Reference", "Status", "Description", "CreatedAt" },
            MeasureColumn = "DebitAmount",
            GroupKeys = new[] { "AccountCode", "Status", "DocumentType" },
            DateColumn = "PostingDate",
            HasTenantId = true
        },
        ["goodsreceipts"] = new EntityDescriptor
        {
            Key = "goodsreceipts",
            DisplayName = "GoodsReceipts",
            Keywords = new[] { "goods receipts", "goods receipt", "grn", "goodsreceipts" },
            Columns = new[] { "GrnNumber", "Date", "PoNumber", "MaterialName", "QtyReceived", "QtyAccepted", "Status" },
            MeasureColumn = null,
            GroupKeys = new[] { "Status", "PoNumber" },
            DateColumn = "Date",
            HasTenantId = true
        },
        ["stockbalances"] = new EntityDescriptor
        {
            Key = "stockbalances",
            DisplayName = "StockBalances",
            Keywords = new[] { "stock balances", "stock balance", "stock", "inventory balances", "inventory" },
            Columns = new[] { "MaterialCode", "MaterialName", "Plant", "StorageLocation", "BatchNumber", "StockType", "Quantity", "UOM", "UnitPrice", "TotalValue", "Status" },
            MeasureColumn = "Quantity",
            GroupKeys = new[] { "MaterialCode", "Plant", "StockType", "Status" },
            DateColumn = null,
            HasTenantId = true
        },
        ["auditlogs"] = new EntityDescriptor
        {
            Key = "auditlogs",
            DisplayName = "AuditLogs",
            Keywords = new[] { "audit logs", "audit log", "audit trail", "auditlogs" },
            Columns = new[] { "ModuleName", "EntityName", "ActionType", "Description", "UserName", "Timestamp" },
            MeasureColumn = null,
            GroupKeys = new[] { "ModuleName", "ActionType", "EntityName" },
            DateColumn = "Timestamp",
            HasTenantId = true
        }
    };

    private readonly YuktiraDbContext _db;
    private readonly IZqmNonConformanceService _ncService;
    private readonly IAuditService _auditService;
    private readonly Nl2SqlOptions _options;

    public Nl2SqlService(
        YuktiraDbContext db,
        IZqmNonConformanceService ncService,
        IAuditService auditService,
        IOptions<Nl2SqlOptions> options)
    {
        _db = db;
        _ncService = ncService;
        _auditService = auditService;
        _options = options?.Value ?? new Nl2SqlOptions();
    }

    public List<string> GetSupportedIntents()
    {
        return new List<string>
        {
            "show 10 purchase orders",
            "show inspection lots with status Created",
            "count sales orders",
            "sum amount of purchase orders",
            "average amount of sales orders",
            "group by status of inspection lots",
            "show stock balances for material MAT-001 at plant 1000",
            "show journal lines for vendor V-100",
            "draft non-conformance ticket if defect rate > 5",
            "draft non-conformance ticket if batch yield loss > 2",
            "draft non-conformance ticket if inspection failure rate > 10"
        };
    }

    public async Task<Nl2SqlQueryResult> QueryAsync(Nl2SqlQueryRequest request)
    {
        var sw = Stopwatch.StartNew();
        var text = request?.Text?.Trim() ?? "";
        var result = new Nl2SqlQueryResult();

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Success = false;
            result.Error = "Query text is required. Example: show 10 purchase orders";
            return result;
        }

        if (ContainsDangerousTokens(text))
        {
            result.Success = false;
            result.Error = "Query rejected: input contains disallowed SQL tokens. Only natural language over whitelisted entities is supported.";
            return result;
        }

        var lower = text.ToLowerInvariant();
        var descriptor = DetectEntity(lower);
        if (descriptor is null)
        {
            result.Success = false;
            result.Error = $"Unsupported entity in query. Supported entities: {string.Join(", ", Entities.Values.Select(v => v.DisplayName))}";
            return result;
        }

        var intent = DetectIntent(lower);
        var limit = DetectLimit(lower, request?.Limit, _options.MaxRows);
        var filters = ExtractFilters(lower, descriptor);
        var groupBy = DetectGroupBy(lower, descriptor);
        var measure = DetectMeasure(lower, descriptor);

        result.Intent = intent;
        result.Entity = descriptor.DisplayName;
        result.Sql = BuildDisplaySql(descriptor, intent, filters, groupBy, measure, limit);

        try
        {
            var (columns, rows) = await ExecuteLinqAsync(descriptor, intent, filters, groupBy, measure, limit, request?.TenantId ?? Guid.Empty, sw);
            result.Columns = columns;
            result.Rows = rows;
            result.RowCount = rows.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Query failed: {ex.Message}";
        }

        sw.Stop();
        result.ExecutedMs = sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<Nl2SqlDraftResult> DraftAsync(Nl2SqlDraftRequest request)
    {
        var text = request?.Text?.Trim() ?? "";
        var result = new Nl2SqlDraftResult();

        if (!_options.AllowDrafting)
        {
            result.Success = false;
            result.Error = "Drafting is disabled by configuration (Nl2Sql:AllowDrafting=false).";
            return result;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Success = false;
            result.Error = "Drafting text is required. Example: draft non-conformance ticket if defect rate > 5";
            return result;
        }

        if (ContainsDangerousTokens(text))
        {
            result.Success = false;
            result.Error = "Drafting request rejected: input contains disallowed SQL tokens.";
            return result;
        }

        var match = DraftRegex().Match(text.ToLowerInvariant());
        if (!match.Success)
        {
            result.Success = false;
            result.Error = "Unsupported drafting intent. Supported pattern: draft non-conformance ticket if <metric> > <pct>. Metrics: batch yield loss, defect rate, inspection failure rate.";
            return result;
        }

        var metric = match.Groups[1].Value.Trim();
        var threshold = decimal.TryParse(match.Groups[2].Value, out var parsed) ? parsed : 0m;
        var tenantId = request?.TenantId ?? Guid.Empty;

        decimal value;
        string metricLabel;
        if (metric.Contains("yield"))
        {
            metricLabel = "batch yield loss";
            value = await ComputeBatchYieldLossAsync(tenantId);
        }
        else if (metric.Contains("failure"))
        {
            metricLabel = "inspection failure rate";
            value = await ComputeInspectionFailureRateAsync(tenantId);
        }
        else
        {
            metricLabel = "defect rate";
            value = await ComputeDefectRateAsync(tenantId);
        }

        result.Condition = $"{metricLabel} > {threshold}";
        result.Value = Math.Round(value, 2);
        result.Threshold = threshold;

        if (result.Value > threshold)
        {
            try
            {
                var nc = await _ncService.CreateNonConformanceAsync(new NonConformanceCreateRequest
                {
                    TenantId = tenantId,
                    NCType = "Defect",
                    Severity = result.Value > threshold * 2m ? "Major" : "Minor",
                    MaterialCode = "NL2SQL",
                    MaterialName = "Analytics auto-draft",
                    BatchNumber = "",
                    Plant = "",
                    InspectionLotNumber = "",
                    DefectCodeGroup = "NL2SQL",
                    DefectCode = "AUTO",
                    DefectDescription = $"Auto-drafted by analytics: {metricLabel} was {result.Value}% which exceeds threshold {threshold}%.",
                    RootCauseCategory = "Analytics",
                    RootCauseDescription = $"Threshold breach detected for {metricLabel}.",
                    DetectedBy = "NL2SQL",
                    VendorCode = "",
                    AffectedQuantity = 0m,
                    EstimatedCost = 0m,
                    Priority = result.Value > threshold * 2m ? "High" : "Medium",
                    UserId = request?.UserId ?? ""
                });

                result.Success = true;
                result.Drafted = nc.Success;
                result.NonConformanceId = nc.Success ? (nc.NCId != Guid.Empty ? nc.NCId.ToString() : nc.NCNumber) : null;
                if (!nc.Success)
                {
                    result.Error = string.Join("; ", nc.Errors);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Failed to draft non-conformance: {ex.Message}";
            }
        }
        else
        {
            result.Success = true;
            result.Drafted = false;
        }

        try
        {
            await _auditService.LogAsync(new AuditEntryDto
            {
                Timestamp = DateTime.UtcNow,
                UserId = Guid.TryParse(request?.UserId, out var uid) ? uid : null,
                TenantId = tenantId,
                ModuleName = "Analytics",
                ActionType = result.Drafted ? ActionType.Create : ActionType.Config,
                EntityName = "Nl2SqlDraft",
                EntityId = result.NonConformanceId,
                Details = $"{result.Condition}; value={result.Value}; drafted={result.Drafted}"
            });
        }
        catch
        {
        }

        return result;
    }

    private async Task<decimal> ComputeDefectRateAsync(Guid tenantId)
    {
        var total = await _db.InspectionResults.AsNoTracking().CountAsync(r => r.TenantId == tenantId);
        if (total == 0) return 0m;
        var failed = await _db.InspectionResults.AsNoTracking().CountAsync(r =>
            r.TenantId == tenantId
            && (r.Evaluation == "Fail" || r.Evaluation == "Failed" || r.Status == "Failed" || r.Result == "Fail"));
        return (decimal)failed / total * 100m;
    }

    private async Task<decimal> ComputeInspectionFailureRateAsync(Guid tenantId)
    {
        var total = await _db.InspectionLots.AsNoTracking().CountAsync(l => l.TenantId == tenantId);
        if (total == 0) return 0m;
        var failed = await _db.InspectionLots.AsNoTracking().CountAsync(l =>
            l.TenantId == tenantId
            && (l.Status == "Failed" || (l.Inspected > 0 && l.Passed < l.Inspected)));
        return (decimal)failed / total * 100m;
    }

    private async Task<decimal> ComputeBatchYieldLossAsync(Guid tenantId)
    {
        var lots = await _db.InspectionLots.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Select(l => new { l.Inspected, l.Failed })
            .ToListAsync();
        var inspected = lots.Sum(l => (decimal)l.Inspected);
        if (inspected <= 0m) return 0m;
        var failed = lots.Sum(l => (decimal)l.Failed);
        return failed / inspected * 100m;
    }

    private static bool ContainsDangerousTokens(string text)
    {
        var padded = $" {text.ToLowerInvariant()} ";
        return DangerousTokens.Any(t => padded.Contains(t, StringComparison.Ordinal) || text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static EntityDescriptor? DetectEntity(string lower)
    {
        foreach (var pair in Entities.OrderByDescending(p => p.Value.Keywords.Max(k => k.Length)))
        {
            foreach (var keyword in pair.Value.Keywords.OrderByDescending(k => k.Length))
            {
                if (lower.Contains(keyword, StringComparison.Ordinal))
                {
                    return pair.Value;
                }
            }
        }
        return null;
    }

    private static string DetectIntent(string lower)
    {
        if (lower.Contains("how many") || lower.Contains("count")) return "count";
        if (lower.Contains("average") || lower.Contains(" avg") || lower.Contains("mean")) return "average";
        if (lower.Contains("sum ") || lower.Contains("total") || lower.Contains("add up")) return "sum";
        if (lower.Contains("group")) return "groupby";
        return "show";
    }

    private static int? DetectLimit(string lower, int? requested, int maxRows)
    {
        var limit = requested;
        var match = NumberRegex().Match(lower);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > 0)
        {
            limit = parsed;
        }
        if (limit is null || limit <= 0) return maxRows;
        return Math.Min(limit.Value, maxRows);
    }

    private static string? DetectGroupBy(string lower, EntityDescriptor descriptor)
    {
        var match = GroupByRegex().Match(lower);
        if (!match.Success) return null;
        var candidate = match.Groups[1].Value.Trim();
        foreach (var key in descriptor.GroupKeys)
        {
            if (string.Equals(key, candidate, StringComparison.OrdinalIgnoreCase)
                || candidate.Contains(key.Replace("_", ""), StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }
        return null;
    }

    private static string? DetectMeasure(string lower, EntityDescriptor descriptor)
    {
        if (descriptor.MeasureColumn is null) return null;
        if (lower.Contains(descriptor.MeasureColumn.Replace("_", " ").ToLowerInvariant())
            || lower.Contains(descriptor.MeasureColumn.ToLowerInvariant()))
        {
            return descriptor.MeasureColumn;
        }
        return descriptor.MeasureColumn;
    }

    private static readonly HashSet<string> FilterStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "of", "for", "in", "with", "and", "by", "on", "at", "to", "from", "into", "over", "under", "per"
    };

    private static List<FilterClause> ExtractFilters(string lower, EntityDescriptor descriptor)
    {
        var filters = new List<FilterClause>();

        var statusMatch = StatusRegex().Match(lower);
        if (statusMatch.Success && !FilterStopwords.Contains(statusMatch.Groups[1].Value.Trim()))
            filters.Add(new FilterClause("Status", statusMatch.Groups[1].Value.Trim()));

        var materialMatch = MaterialRegex().Match(lower);
        if (materialMatch.Success && descriptor.Columns.Contains("MaterialCode"))
            filters.Add(new FilterClause("MaterialCode", materialMatch.Groups[1].Value.Trim()));

        var vendorMatch = VendorRegex().Match(lower);
        if (vendorMatch.Success && descriptor.Columns.Contains("VendorCode"))
            filters.Add(new FilterClause("VendorCode", vendorMatch.Groups[1].Value.Trim()));

        var plantMatch = PlantRegex().Match(lower);
        if (plantMatch.Success && descriptor.Columns.Contains("Plant"))
            filters.Add(new FilterClause("Plant", plantMatch.Groups[1].Value.Trim()));

        if (descriptor.DateColumn is not null)
        {
            var fromMatch = FromDateRegex().Match(lower);
            if (fromMatch.Success && DateTime.TryParse(fromMatch.Groups[1].Value, out var fromDate))
                filters.Add(new FilterClause(descriptor.DateColumn, fromDate.ToString("yyyy-MM-dd"), ">="));

            var toMatch = ToDateRegex().Match(lower);
            if (toMatch.Success && DateTime.TryParse(toMatch.Groups[1].Value, out var toDate))
                filters.Add(new FilterClause(descriptor.DateColumn, toDate.ToString("yyyy-MM-dd"), "<="));
        }

        return filters;
    }

    private static string BuildDisplaySql(EntityDescriptor descriptor, string intent, List<FilterClause> filters, string? groupBy, string? measure, int? limit)
    {
        var cols = descriptor.Columns.Take(8).Select(c => $"\"{c}\"");
        string selectClause = intent switch
        {
            "count" => "COUNT(*) AS \"Count\"",
            "sum" => $"SUM(\"{measure ?? descriptor.MeasureColumn}\") AS \"Total\"",
            "average" => $"AVG(\"{measure ?? descriptor.MeasureColumn}\") AS \"Average\"",
            "groupby" => $"\"{groupBy ?? descriptor.GroupKeys[0]}\", COUNT(*) AS \"Count\"",
            _ => string.Join(", ", cols)
        };

        var sql = $"SELECT {selectClause} FROM \"{descriptor.DisplayName}\"";
        var where = new List<string>();
        if (descriptor.HasTenantId) where.Add("\"TenantId\" = @tenantId");
        foreach (var f in filters)
        {
            where.Add($"\"{f.Field}\" {f.Op} @{f.Field}");
        }
        if (where.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", where);
        }
        if (intent == "groupby")
        {
            sql += $" GROUP BY \"{groupBy ?? descriptor.GroupKeys[0]}\"";
        }
        if (intent == "show")
        {
            sql += " ORDER BY \"CreatedAt\" DESC";
        }
        sql += $" LIMIT {limit ?? descriptor.Columns.Length}";
        return sql;
    }

    private async Task<(List<string> columns, List<Dictionary<string, object?>> rows)> ExecuteLinqAsync(
        EntityDescriptor descriptor,
        string intent,
        List<FilterClause> filters,
        string? groupBy,
        string? measure,
        int? limit,
        Guid tenantId,
        Stopwatch sw)
    {
        var take = limit ?? _options.MaxRows;

        switch (descriptor.Key)
        {
            case "purchaseorders":
            {
                var q = _db.PurchaseOrders.AsQueryable();
                if (descriptor.HasTenantId) q = q.Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "VendorCode" => x.VendorCode == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.Date >= d : x.Date <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var total = await q.SumAsync(x => x.Amount);
                    return (new List<string> { "TotalAmount" }, new List<Dictionary<string, object?>> { new() { ["TotalAmount"] = total } });
                }
                if (intent == "average")
                {
                    var avg = await q.CountAsync() == 0 ? 0m : await q.AverageAsync(x => x.Amount);
                    return (new List<string> { "AverageAmount" }, new List<Dictionary<string, object?>> { new() { ["AverageAmount"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key switch
                    {
                        "VendorCode" => await q.GroupBy(x => x.VendorCode).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.Date).Take(take).ToListAsync();
                var columns = new List<string> { "PoNumber", "VendorCode", "Status", "Amount", "Date" };
                var poRows = projected.Select(x => new Dictionary<string, object?>
                {
                    ["PoNumber"] = x.PoNumber,
                    ["VendorCode"] = x.VendorCode,
                    ["Status"] = x.Status,
                    ["Amount"] = x.Amount,
                    ["Date"] = x.Date
                }).ToList();
                return (columns, poRows);
            }
            case "salesorders":
            {
                var q = _db.SalesOrders.AsQueryable();
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "CustomerName" => x.CustomerName == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.OrderDate >= d : x.OrderDate <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var total = await q.SumAsync(x => x.Amount);
                    return (new List<string> { "TotalAmount" }, new List<Dictionary<string, object?>> { new() { ["TotalAmount"] = total } });
                }
                if (intent == "average")
                {
                    var avg = await q.CountAsync() == 0 ? 0m : await q.AverageAsync(x => x.Amount);
                    return (new List<string> { "AverageAmount" }, new List<Dictionary<string, object?>> { new() { ["AverageAmount"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key == "CustomerName"
                        ? await q.GroupBy(x => x.CustomerName).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                        : await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync();
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.OrderDate).Take(take).ToListAsync();
                return (
                    new List<string> { "OrderNumber", "CustomerName", "Status", "Amount", "OrderDate" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["OrderNumber"] = x.OrderNumber,
                        ["CustomerName"] = x.CustomerName,
                        ["Status"] = x.Status,
                        ["Amount"] = x.Amount,
                        ["OrderDate"] = x.OrderDate
                    }).ToList());
            }
            case "inspectionlots":
            {
                var q = _db.InspectionLots.AsQueryable().Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "MaterialCode" => x.MaterialCode == f.Value,
                        "Plant" => x.Plant == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.CreatedAt >= d : x.CreatedAt <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var lotRows = await q.ToListAsync();
                    var total = lotRows.Sum(x => decimal.TryParse(x.Quantity, out var v) ? v : 0m);
                    return (new List<string> { "TotalQuantity" }, new List<Dictionary<string, object?>> { new() { ["TotalQuantity"] = total } });
                }
                if (intent == "average")
                {
                    var lotRows = await q.ToListAsync();
                    var avg = lotRows.Count == 0 ? 0m : lotRows.Average(x => decimal.TryParse(x.Quantity, out var v) ? v : 0m);
                    return (new List<string> { "AverageQuantity" }, new List<Dictionary<string, object?>> { new() { ["AverageQuantity"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key switch
                    {
                        "MaterialCode" => await q.GroupBy(x => x.MaterialCode).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "Plant" => await q.GroupBy(x => x.Plant).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
                return (
                    new List<string> { "LotNumber", "MaterialCode", "Plant", "Status", "Quantity", "CreatedAt" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["LotNumber"] = x.LotNumber,
                        ["MaterialCode"] = x.MaterialCode,
                        ["Plant"] = x.Plant,
                        ["Status"] = x.Status,
                        ["Quantity"] = x.Quantity,
                        ["CreatedAt"] = x.CreatedAt
                    }).ToList());
            }
            case "inspectionresults":
            {
                var q = _db.InspectionResults.AsQueryable().Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "Evaluation" => x.Evaluation == f.Value,
                        "LotNumber" => x.LotNumber == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.CreatedAt >= d : x.CreatedAt <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var total = await q.SumAsync(x => x.MeasuredValue);
                    return (new List<string> { "TotalMeasuredValue" }, new List<Dictionary<string, object?>> { new() { ["TotalMeasuredValue"] = total } });
                }
                if (intent == "average")
                {
                    var avg = await q.CountAsync() == 0 ? 0m : await q.AverageAsync(x => x.MeasuredValue);
                    return (new List<string> { "AverageMeasuredValue" }, new List<Dictionary<string, object?>> { new() { ["AverageMeasuredValue"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key switch
                    {
                        "Evaluation" => await q.GroupBy(x => x.Evaluation).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "LotNumber" => await q.GroupBy(x => x.LotNumber).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
                return (
                    new List<string> { "ResultId", "LotNumber", "Characteristic", "Evaluation", "MeasuredValue", "Status", "CreatedAt" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["ResultId"] = x.ResultId,
                        ["LotNumber"] = x.LotNumber,
                        ["Characteristic"] = x.Characteristic,
                        ["Evaluation"] = x.Evaluation,
                        ["MeasuredValue"] = x.MeasuredValue,
                        ["Status"] = x.Status,
                        ["CreatedAt"] = x.CreatedAt
                    }).ToList());
            }
            case "universaljournals":
            {
                var q = _db.UniversalJournals.AsQueryable().Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "VendorCode" => x.VendorCode == f.Value,
                        "MaterialCode" => x.MaterialCode == f.Value,
                        "AccountCode" => x.AccountCode == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.PostingDate >= d : x.PostingDate <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var total = await q.SumAsync(x => x.DebitAmount);
                    return (new List<string> { "TotalDebitAmount" }, new List<Dictionary<string, object?>> { new() { ["TotalDebitAmount"] = total } });
                }
                if (intent == "average")
                {
                    var avg = await q.CountAsync() == 0 ? 0m : await q.AverageAsync(x => x.DebitAmount);
                    return (new List<string> { "AverageDebitAmount" }, new List<Dictionary<string, object?>> { new() { ["AverageDebitAmount"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key switch
                    {
                        "AccountCode" => await q.GroupBy(x => x.AccountCode).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "DocumentType" => await q.GroupBy(x => x.DocumentType).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.PostingDate).Take(take).ToListAsync();
                return (
                    new List<string> { "DocumentNumber", "AccountCode", "AccountName", "DebitAmount", "CreditAmount", "VendorCode", "Status" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["DocumentNumber"] = x.DocumentNumber,
                        ["AccountCode"] = x.AccountCode,
                        ["AccountName"] = x.AccountName,
                        ["DebitAmount"] = x.DebitAmount,
                        ["CreditAmount"] = x.CreditAmount,
                        ["VendorCode"] = x.VendorCode,
                        ["Status"] = x.Status
                    }).ToList());
            }
            case "goodsreceipts":
            {
                var q = _db.GoodsReceipts.AsQueryable().Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "PoNumber" => x.PoNumber == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.Date >= d : x.Date <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "Status";
                    var groups = key == "PoNumber"
                        ? await q.GroupBy(x => x.PoNumber).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                        : await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync();
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.Date).Take(take).ToListAsync();
                return (
                    new List<string> { "GrnNumber", "PoNumber", "MaterialName", "Status", "Date" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["GrnNumber"] = x.GrnNumber,
                        ["PoNumber"] = x.PoNumber,
                        ["MaterialName"] = x.MaterialName,
                        ["Status"] = x.Status,
                        ["Date"] = x.Date
                    }).ToList());
            }
            case "stockbalances":
            {
                var q = _db.StockBalances.AsQueryable().Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "Status" => x.Status == f.Value,
                        "MaterialCode" => x.MaterialCode == f.Value,
                        "Plant" => x.Plant == f.Value,
                        _ => true
                    });

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "sum")
                {
                    var total = await q.SumAsync(x => x.Quantity);
                    return (new List<string> { "TotalQuantity" }, new List<Dictionary<string, object?>> { new() { ["TotalQuantity"] = total } });
                }
                if (intent == "average")
                {
                    var avg = await q.CountAsync() == 0 ? 0m : await q.AverageAsync(x => x.Quantity);
                    return (new List<string> { "AverageQuantity" }, new List<Dictionary<string, object?>> { new() { ["AverageQuantity"] = avg } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "MaterialCode";
                    var groups = key switch
                    {
                        "Plant" => await q.GroupBy(x => x.Plant).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "StockType" => await q.GroupBy(x => x.StockType).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "Status" => await q.GroupBy(x => x.Status).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.MaterialCode).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.Take(take).ToListAsync();
                return (
                    new List<string> { "MaterialCode", "Plant", "StockType", "Quantity", "Status" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["MaterialCode"] = x.MaterialCode,
                        ["Plant"] = x.Plant,
                        ["StockType"] = x.StockType,
                        ["Quantity"] = x.Quantity,
                        ["Status"] = x.Status
                    }).ToList());
            }
            case "auditlogs":
            {
                var q = _db.AuditLogs.AsQueryable();
                if (descriptor.HasTenantId) q = q.Where(x => x.TenantId == tenantId);
                q = ApplyStringFilters(q, filters, (x, f) =>
                    f.Field switch
                    {
                        "ModuleName" => x.ModuleName == f.Value,
                        "ActionType" => x.ActionType == f.Value,
                        "EntityName" => x.EntityName == f.Value,
                        _ => true
                    });
                q = ApplyDateFilter(q, filters, descriptor.DateColumn, (x, d, op) => op == ">=" ? x.Timestamp >= d : x.Timestamp <= d);

                if (intent == "count")
                {
                    return (new List<string> { "Count" }, new List<Dictionary<string, object?>> { new() { ["Count"] = await q.CountAsync() } });
                }
                if (intent == "groupby")
                {
                    var key = groupBy ?? "ModuleName";
                    var groups = key switch
                    {
                        "ActionType" => await q.GroupBy(x => x.ActionType).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        "EntityName" => await q.GroupBy(x => x.EntityName).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(),
                        _ => await q.GroupBy(x => x.ModuleName).Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync()
                    };
                    var rows = groups.Select(g => new Dictionary<string, object?> { [key] = g.Key, ["Count"] = g.Count }).ToList();
                    return (new List<string> { key, "Count" }, rows);
                }

                var projected = await q.OrderByDescending(x => x.Timestamp).Take(take).ToListAsync();
                return (
                    new List<string> { "ModuleName", "EntityName", "ActionType", "Description", "Timestamp" },
                    projected.Select(x => new Dictionary<string, object?>
                    {
                        ["ModuleName"] = x.ModuleName,
                        ["EntityName"] = x.EntityName,
                        ["ActionType"] = x.ActionType,
                        ["Description"] = x.Description,
                        ["Timestamp"] = x.Timestamp
                    }).ToList());
            }
            default:
                return (new List<string>(), new List<Dictionary<string, object?>>());
        }
    }

    private static IQueryable<T> ApplyStringFilters<T>(IQueryable<T> query, List<FilterClause> filters, Func<T, FilterClause, bool> predicate)
    {
        var list = filters.Where(f => f.Op == "=").ToList();
        if (list.Count == 0) return query;
        var array = list.ToArray();
        return query.Where(x => array.All(f => predicate(x, f)));
    }

    private static IQueryable<T> ApplyDateFilter<T>(IQueryable<T> query, List<FilterClause> filters, string? dateColumn, Func<T, DateTime, string, bool> predicate)
    {
        if (dateColumn is null) return query;
        var dateFilters = filters.Where(f => string.Equals(f.Field, dateColumn, StringComparison.OrdinalIgnoreCase) && f.Op is ">=" or "<=").ToList();
        if (dateFilters.Count == 0) return query;
        foreach (var f in dateFilters)
        {
            if (DateTime.TryParse(f.Value, out var dt))
            {
                var op = f.Op;
                var captured = dt;
                query = query.Where(x => predicate(x, captured, op));
            }
        }
        return query;
    }

    [GeneratedRegex(@"\b(\d+)\b")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"status\s+(?:=\s*)?([a-z0-9_-]+)")]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"material\s+([a-z0-9_-]+)")]
    private static partial Regex MaterialRegex();

    [GeneratedRegex(@"vendor\s+([a-z0-9_-]+)")]
    private static partial Regex VendorRegex();

    [GeneratedRegex(@"plant\s+([a-z0-9_-]+)")]
    private static partial Regex PlantRegex();

    [GeneratedRegex(@"from\s+(\d{4}-\d{2}-\d{2})")]
    private static partial Regex FromDateRegex();

    [GeneratedRegex(@"to\s+(\d{4}-\d{2}-\d{2})")]
    private static partial Regex ToDateRegex();

    [GeneratedRegex(@"group(?:\s+by)?\s+([a-z_]+)")]
    private static partial Regex GroupByRegex();

    [GeneratedRegex(@"draft\s+non[- ]conformance\s+ticket\s+if\s+(.+?)\s*>\s*([\d.]+)")]
    private static partial Regex DraftRegex();

    private sealed class EntityDescriptor
    {
        public string Key { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string[] Keywords { get; init; } = Array.Empty<string>();
        public string[] Columns { get; init; } = Array.Empty<string>();
        public string? MeasureColumn { get; init; }
        public string[] GroupKeys { get; init; } = Array.Empty<string>();
        public string? DateColumn { get; init; }
        public bool HasTenantId { get; init; } = true;
    }

    private sealed class FilterClause
    {
        public FilterClause(string field, string value, string op = "=")
        {
            Field = field;
            Value = value;
            Op = op;
        }

        public string Field { get; }
        public string Value { get; }
        public string Op { get; }
    }
}
