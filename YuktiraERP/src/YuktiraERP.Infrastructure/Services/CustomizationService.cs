using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class CustomizationService : ICustomizationService
{
    private static readonly Dictionary<string, List<ColumnDefinitionDto>> _layouts = new();

    public Task<List<ColumnDefinitionDto>> GetScreenLayoutAsync(Guid userId, Guid tenantId, string screenName)
    {
        var key = $"{userId}:{tenantId}:{screenName}";
        return Task.FromResult(_layouts.GetValueOrDefault(key, GetDefaultColumns(screenName)));
    }

    public Task<bool> SaveUserLayoutAsync(Guid userId, Guid tenantId, string screenName, List<ColumnDefinitionDto> columns)
    {
        _layouts[$"{userId}:{tenantId}:{screenName}"] = columns;
        return Task.FromResult(true);
    }

    public Task<bool> AddColumnAsync(Guid tenantId, string screenName, ColumnDefinitionDto column)
    {
        var key = $":{tenantId}:{screenName}";
        if (!_layouts.ContainsKey(key))
            _layouts[key] = GetDefaultColumns(screenName);
        var cols = _layouts[key];
        if (!cols.Any(c => c.ColumnName == column.ColumnName))
        {
            column.OrderIndex = cols.Count + 1;
            cols.Add(column);
        }
        return Task.FromResult(true);
    }

    public Task<bool> DeleteColumnAsync(Guid tenantId, string screenName, string columnName)
    {
        var key = $":{tenantId}:{screenName}";
        if (_layouts.TryGetValue(key, out var cols))
        {
            var col = cols.FirstOrDefault(c => c.ColumnName == columnName);
            if (col != null)
                cols.Remove(col);
        }
        return Task.FromResult(true);
    }
    public Task<bool> ResetLayoutAsync(Guid userId, Guid tenantId, string screenName)
    {
        _layouts.Remove($"{userId}:{tenantId}:{screenName}");
        return Task.FromResult(true);
    }

    private static List<ColumnDefinitionDto> GetDefaultColumns(string screenName) => screenName switch
    {
        "PO_LIST" => new List<ColumnDefinitionDto>
        {
            new() { ColumnName = "po_number", ColumnLabel = "PO Number", DataType = "TEXT", Width = 150, OrderIndex = 1 },
            new() { ColumnName = "po_date", ColumnLabel = "Date", DataType = "DATE", Width = 120, OrderIndex = 2 },
            new() { ColumnName = "vendor_name", ColumnLabel = "Vendor", DataType = "TEXT", Width = 200, OrderIndex = 3 },
            new() { ColumnName = "total_amount", ColumnLabel = "Amount", DataType = "DECIMAL", Width = 150, OrderIndex = 4 },
            new() { ColumnName = "status", ColumnLabel = "Status", DataType = "TEXT", Width = 120, OrderIndex = 5 }
        },
        "SO_LIST" => new List<ColumnDefinitionDto>
        {
            new() { ColumnName = "so_number", ColumnLabel = "SO Number", DataType = "TEXT", Width = 150, OrderIndex = 1 },
            new() { ColumnName = "so_date", ColumnLabel = "Date", DataType = "DATE", Width = 120, OrderIndex = 2 },
            new() { ColumnName = "customer_name", ColumnLabel = "Customer", DataType = "TEXT", Width = 200, OrderIndex = 3 },
            new() { ColumnName = "grand_total", ColumnLabel = "Total", DataType = "DECIMAL", Width = 150, OrderIndex = 4 },
            new() { ColumnName = "status", ColumnLabel = "Status", DataType = "TEXT", Width = 120, OrderIndex = 5 }
        },
        _ => new List<ColumnDefinitionDto>
        {
            new() { ColumnName = "id", ColumnLabel = "ID", DataType = "TEXT", Width = 100, OrderIndex = 1 },
            new() { ColumnName = "code", ColumnLabel = "Code", DataType = "TEXT", Width = 120, OrderIndex = 2 },
            new() { ColumnName = "name", ColumnLabel = "Name", DataType = "TEXT", Width = 200, OrderIndex = 3 },
            new() { ColumnName = "status", ColumnLabel = "Status", DataType = "TEXT", Width = 100, OrderIndex = 4 }
        }
    };
}
