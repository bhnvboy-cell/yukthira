using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class ReportingQueryService : IReportingQueryService
{
    public PagedQueryResult<T> ExecuteQuery<T>(IQueryable<T> source, DynamicQueryRequest request)
    {
        IEnumerable<T> query = source;

        // 1. Filtering
        foreach (var filter in request.Filters)
        {
            if (string.IsNullOrWhiteSpace(filter.FieldName)) continue;

            var prop = typeof(T).GetProperty(filter.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) continue;

            query = query.Where(item => MatchesFilter(item, prop, filter.Operator, filter.Value));
        }

        int totalCount = query.Count();

        // 2. Sorting
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortProp = typeof(T).GetProperty(request.SortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (sortProp != null)
            {
                query = request.SortDescending
                    ? query.OrderByDescending(item => sortProp.GetValue(item))
                    : query.OrderBy(item => sortProp.GetValue(item));
            }
        }

        // 3. Pagination
        var items = query.Skip(request.Skip).Take(request.Take > 0 ? request.Take : 50).ToList();

        return new PagedQueryResult<T>
        {
            TotalCount = totalCount,
            PageIndex = request.Take > 0 ? (request.Skip / request.Take) : 0,
            PageSize = request.Take,
            Items = items
        };
    }

    private static bool MatchesFilter<T>(T item, PropertyInfo prop, string op, string rawValue)
    {
        var propVal = prop.GetValue(item);
        if (propVal == null)
            return string.Equals(op, "neq", StringComparison.OrdinalIgnoreCase);

        string stringVal = propVal.ToString() ?? string.Empty;

        switch (op.ToLowerInvariant())
        {
            case "eq":
                return string.Equals(stringVal, rawValue, StringComparison.OrdinalIgnoreCase);
            case "neq":
                return !string.Equals(stringVal, rawValue, StringComparison.OrdinalIgnoreCase);
            case "contains":
                return stringVal.Contains(rawValue, StringComparison.OrdinalIgnoreCase);
            case "startswith":
                return stringVal.StartsWith(rawValue, StringComparison.OrdinalIgnoreCase);
            case "gt":
            case "gte":
            case "lt":
            case "lte":
                if (decimal.TryParse(stringVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numVal) &&
                    decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal targetNum))
                {
                    return op switch
                    {
                        "gt" => numVal > targetNum,
                        "gte" => numVal >= targetNum,
                        "lt" => numVal < targetNum,
                        "lte" => numVal <= targetNum,
                        _ => false
                    };
                }
                return false;
            default:
                return true;
        }
    }
}
