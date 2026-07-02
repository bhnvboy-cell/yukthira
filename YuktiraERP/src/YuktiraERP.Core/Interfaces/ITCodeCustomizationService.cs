namespace YuktiraERP.Core.Interfaces;

public interface ITCodeCustomizationService
{
    Task<List<CustomFieldDto>> GetCustomFieldsAsync(Guid tenantId, string tcode);
    Task<CustomFieldDto> AddCustomFieldAsync(Guid tenantId, string tcode, CustomFieldDto field);
    Task<bool> UpdateCustomFieldAsync(Guid tenantId, Guid fieldId, CustomFieldDto field);
    Task<bool> DeleteCustomFieldAsync(Guid tenantId, Guid fieldId);
    Task<List<LayoutFieldDto>> GetLayoutAsync(Guid tenantId, string tcode);
    Task<bool> SaveLayoutAsync(Guid tenantId, string tcode, List<LayoutFieldDto> layout);
}

public class CustomFieldDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = "";
    public string FieldLabel { get; set; } = "";
    public string DataType { get; set; } = "TEXT";
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public string DefaultValue { get; set; } = "";
    public string ValidationRuleJson { get; set; } = "{}";
    public string ConditionalVisibilityJson { get; set; } = "{}";
}

public class LayoutFieldDto
{
    public string FieldName { get; set; } = "";
    public string SectionName { get; set; } = "General";
    public int OrderIndex { get; set; }
    public int Width { get; set; } = 200;
    public bool IsFrozen { get; set; }
}
