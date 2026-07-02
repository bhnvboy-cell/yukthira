using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class TCodeCustomizationService : ITCodeCustomizationService
{
    private readonly YuktiraDbContext _db;

    public TCodeCustomizationService(YuktiraDbContext db) => _db = db;

    public async Task<List<CustomFieldDto>> GetCustomFieldsAsync(Guid tenantId, string tcode)
    {
        return await _db.CustomizationTCodeFields
            .Where(f => f.TenantId == tenantId && f.TCode == tcode)
            .Select(f => new CustomFieldDto
            {
                Id = f.Id,
                FieldName = f.FieldName,
                FieldLabel = f.FieldLabel,
                DataType = f.DataType,
                IsRequired = f.IsRequired,
                IsVisible = f.IsVisible,
                DefaultValue = f.DefaultValue,
                ValidationRuleJson = f.ValidationRuleJson,
                ConditionalVisibilityJson = f.ConditionalVisibilityJson
            }).ToListAsync();
    }

    public async Task<CustomFieldDto> AddCustomFieldAsync(Guid tenantId, string tcode, CustomFieldDto field)
    {
        var entity = new CustomizationTCodeFieldEntity
        {
            TenantId = tenantId,
            TCode = tcode,
            FieldName = field.FieldName.ToLower().Replace(" ", "_"),
            FieldLabel = field.FieldLabel,
            DataType = field.DataType,
            IsRequired = field.IsRequired,
            IsVisible = field.IsVisible,
            DefaultValue = field.DefaultValue,
            ValidationRuleJson = field.ValidationRuleJson ?? "{}",
            ConditionalVisibilityJson = field.ConditionalVisibilityJson ?? "{}"
        };
        _db.CustomizationTCodeFields.Add(entity);
        await _db.SaveChangesAsync();

        _db.CustomizationTCodeLayouts.Add(new CustomizationTCodeLayoutEntity
        {
            TenantId = tenantId,
            TCode = tcode,
            FieldName = entity.FieldName,
            SectionName = "General",
            OrderIndex = await _db.CustomizationTCodeLayouts.Where(l => l.TCode == tcode).CountAsync() + 1,
            Width = 200
        });
        await _db.SaveChangesAsync();

        field.Id = entity.Id;
        return field;
    }

    public async Task<bool> UpdateCustomFieldAsync(Guid tenantId, Guid fieldId, CustomFieldDto field)
    {
        var entity = await _db.CustomizationTCodeFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId);
        if (entity == null) return false;
        entity.FieldLabel = field.FieldLabel;
        entity.DataType = field.DataType;
        entity.IsRequired = field.IsRequired;
        entity.IsVisible = field.IsVisible;
        entity.DefaultValue = field.DefaultValue;
        entity.ValidationRuleJson = field.ValidationRuleJson ?? "{}";
        entity.ConditionalVisibilityJson = field.ConditionalVisibilityJson ?? "{}";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCustomFieldAsync(Guid tenantId, Guid fieldId)
    {
        var entity = await _db.CustomizationTCodeFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId);
        if (entity == null) return false;
        _db.CustomizationTCodeFields.Remove(entity);
        var layouts = await _db.CustomizationTCodeLayouts
            .Where(l => l.TenantId == tenantId && l.TCode == entity.TCode && l.FieldName == entity.FieldName)
            .ToListAsync();
        _db.CustomizationTCodeLayouts.RemoveRange(layouts);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<LayoutFieldDto>> GetLayoutAsync(Guid tenantId, string tcode)
    {
        return await _db.CustomizationTCodeLayouts
            .Where(l => l.TenantId == tenantId && l.TCode == tcode)
            .OrderBy(l => l.OrderIndex)
            .Select(l => new LayoutFieldDto
            {
                FieldName = l.FieldName,
                SectionName = l.SectionName,
                OrderIndex = l.OrderIndex,
                Width = l.Width,
                IsFrozen = l.IsFrozen
            }).ToListAsync();
    }

    public async Task<bool> SaveLayoutAsync(Guid tenantId, string tcode, List<LayoutFieldDto> layout)
    {
        var existing = await _db.CustomizationTCodeLayouts
            .Where(l => l.TenantId == tenantId && l.TCode == tcode).ToListAsync();
        _db.CustomizationTCodeLayouts.RemoveRange(existing);

        for (int i = 0; i < layout.Count; i++)
        {
            _db.CustomizationTCodeLayouts.Add(new CustomizationTCodeLayoutEntity
            {
                TenantId = tenantId,
                TCode = tcode,
                FieldName = layout[i].FieldName,
                SectionName = layout[i].SectionName,
                OrderIndex = i + 1,
                Width = layout[i].Width,
                IsFrozen = layout[i].IsFrozen
            });
        }
        await _db.SaveChangesAsync();
        return true;
    }
}
