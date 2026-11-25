using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace YuktiraERP.Core.Interfaces;

public class QueryFilterCriteria
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // eq, neq, gt, gte, lt, lte, contains, startswith
    public string Value { get; set; } = string.Empty;
}

public class DynamicQueryRequest
{
    public List<QueryFilterCriteria> Filters { get; set; } = new List<QueryFilterCriteria>();
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class PagedQueryResult<T>
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new List<T>();
}

public interface IReportingQueryService
{
    /// <summary>
    /// Executes dynamic in-memory filtering, sorting, and pagination (OData-style abstraction).
    /// </summary>
    PagedQueryResult<T> ExecuteQuery<T>(IQueryable<T> source, DynamicQueryRequest request);
}
