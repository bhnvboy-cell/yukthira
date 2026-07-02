namespace YuktiraERP.Core.Interfaces;

public class ColumnDefinitionDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string ColumnLabel { get; set; } = string.Empty;
    public string DataType { get; set; } = "TEXT";
    public int Width { get; set; } = 150;
    public int OrderIndex { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsFrozen { get; set; }
    public string? Formula { get; set; }
    public string? ConditionalFormatJson { get; set; }
}

public interface ICustomizationService
{
    Task<List<ColumnDefinitionDto>> GetScreenLayoutAsync(Guid userId, Guid tenantId, string screenName);
    Task<bool> SaveUserLayoutAsync(Guid userId, Guid tenantId, string screenName, List<ColumnDefinitionDto> columns);
    Task<bool> AddColumnAsync(Guid tenantId, string screenName, ColumnDefinitionDto column);
    Task<bool> DeleteColumnAsync(Guid tenantId, string screenName, string columnName);
    Task<bool> ResetLayoutAsync(Guid userId, Guid tenantId, string screenName);
}
