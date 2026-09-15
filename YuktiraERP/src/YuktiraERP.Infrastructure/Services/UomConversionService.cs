using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class UomConversionService : IUomConversionService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public UomConversionService(YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<UomConversionResult> ConvertAsync(UomConversionRequest request, Guid tenantId)
    {
        if (request.SourceValue < 0)
            return new UomConversionResult { Success = false, Message = "Source value cannot be negative" };

        if (string.IsNullOrWhiteSpace(request.SourceUomCode) || string.IsNullOrWhiteSpace(request.TargetUomCode))
            return new UomConversionResult { Success = false, Message = "Source and target UoM codes are required" };

        var sourceCode = request.SourceUomCode.ToUpperInvariant();
        var targetCode = request.TargetUomCode.ToUpperInvariant();

        if (sourceCode == targetCode)
        {
            return new UomConversionResult
            {
                Success = true,
                SourceValue = request.SourceValue,
                SourceUomCode = sourceCode,
                TargetValue = Math.Round(request.SourceValue, 2),
                TargetUomCode = targetCode,
                FactorUsed = 1,
                Message = "Same UoM - no conversion needed"
            };
        }

        var sourceUom = await _db.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Msehi == sourceCode && u.IsActive);

        var targetUom = await _db.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Msehi == targetCode && u.IsActive);

        if (sourceUom == null)
            return new UomConversionResult { Success = false, Message = $"Source UoM '{sourceCode}' not found or inactive" };

        if (targetUom == null)
            return new UomConversionResult { Success = false, Message = $"Target UoM '{targetCode}' not found or inactive" };

        if (sourceUom.DimensionCode != targetUom.DimensionCode)
            return new UomConversionResult { Success = false, Message = $"Cannot convert between different dimensions: {sourceUom.DimensionCode} -> {targetUom.DimensionCode}" };

        decimal factorUsed;

        if (!string.IsNullOrWhiteSpace(request.MaterialCode))
        {
            var matCode = request.MaterialCode.ToUpperInvariant();
            var materialConversion = await _db.MaterialUomConversions
                .FirstOrDefaultAsync(c => c.TenantId == tenantId
                    && c.MaterialCode == matCode
                    && c.SourceUomCode == sourceCode
                    && c.TargetUomCode == targetCode
                    && c.IsActive);

            if (materialConversion != null)
            {
                decimal targetValue;
                if (materialConversion.DensityFactor.HasValue && materialConversion.DensityFactor.Value != 0)
                {
                    targetValue = request.SourceValue * materialConversion.ConversionFactor * materialConversion.DensityFactor.Value;
                }
                else
                {
                    targetValue = request.SourceValue * materialConversion.ConversionFactor;
                }

                targetValue = Math.Round(targetValue, targetUom.Decimals);
                factorUsed = materialConversion.ConversionFactor;

                await _audit.LogAsync(new AuditEntryDto
                {
                    TenantId = tenantId,
                    ModuleName = "CORE_UOM",
                    ActionType = Core.Domain.Common.ActionType.Config,
                    EntityName = "UomConversion",
                    EntityId = materialConversion.Id.ToString(),
                    Details = $"Material-specific conversion: {sourceCode} -> {targetCode} for material {matCode}, factor={factorUsed}"
                });

                return new UomConversionResult
                {
                    Success = true,
                    SourceValue = request.SourceValue,
                    SourceUomCode = sourceCode,
                    TargetValue = targetValue,
                    TargetUomCode = targetCode,
                    FactorUsed = factorUsed,
                    Message = "Converted using material-specific factor"
                };
            }
        }

        decimal sourceFactor = sourceUom.Numerator / sourceUom.Denominator;
        decimal targetFactor = targetUom.Numerator / targetUom.Denominator;

        if (targetFactor == 0)
            return new UomConversionResult { Success = false, Message = "Target UoM has zero denominator" };

        decimal convertedValue = ((request.SourceValue * sourceFactor) + sourceUom.AddOffset) / targetFactor - targetUom.AddOffset;
        convertedValue = Math.Round(convertedValue, targetUom.Decimals);

        factorUsed = sourceFactor / targetFactor;

        await _audit.LogAsync(new AuditEntryDto
        {
            TenantId = tenantId,
            ModuleName = "CORE_UOM",
            ActionType = Core.Domain.Common.ActionType.Config,
            EntityName = "UomConversion",
            Details = $"Standard conversion: {sourceCode} -> {targetCode}, factor={factorUsed}"
        });

        return new UomConversionResult
        {
            Success = true,
            SourceValue = request.SourceValue,
            SourceUomCode = sourceCode,
            TargetValue = convertedValue,
            TargetUomCode = targetCode,
            FactorUsed = factorUsed,
            Message = "Converted using standard UoM conversion"
        };
    }

    public async Task<List<UnitOfMeasureEntity>> GetActiveUomsAsync(Guid tenantId)
    {
        return await _db.UnitsOfMeasure
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.Msehi)
            .ToListAsync();
    }

    public async Task<UnitOfMeasureEntity?> GetUomByCodeAsync(string msehi, Guid tenantId)
    {
        var code = msehi.ToUpperInvariant();
        return await _db.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Msehi == code);
    }

    public async Task<UnitOfMeasureEntity> CreateOrUpdateUomAsync(UomCreateUpdateRequest request, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(request.Msehi))
            throw new InvalidOperationException("UoM code (MSEHI) is required");

        var code = request.Msehi.ToUpperInvariant();
        var existing = await _db.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Msehi == code);

        if (existing != null)
        {
            existing.IsoCode = request.IsoCode;
            existing.DimensionCode = request.DimensionCode;
            existing.Numerator = request.Numerator;
            existing.Denominator = request.Denominator;
            existing.AddOffset = request.AddOffset;
            existing.Decimals = request.Decimals;
            existing.ShortText = request.ShortText;
            existing.IsActive = request.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(new AuditEntryDto
            {
                TenantId = tenantId,
                ModuleName = "CORE_UOM",
                ActionType = Core.Domain.Common.ActionType.Update,
                EntityName = "UnitOfMeasure",
                EntityId = existing.Id.ToString(),
                Details = $"Updated UoM: {code}"
            });

            return existing;
        }

        var entity = new UnitOfMeasureEntity
        {
            TenantId = tenantId,
            Msehi = code,
            IsoCode = request.IsoCode,
            DimensionCode = request.DimensionCode,
            Numerator = request.Numerator,
            Denominator = request.Denominator,
            AddOffset = request.AddOffset,
            Decimals = request.Decimals,
            ShortText = request.ShortText,
            IsActive = request.IsActive
        };

        _db.UnitsOfMeasure.Add(entity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            TenantId = tenantId,
            ModuleName = "CORE_UOM",
            ActionType = Core.Domain.Common.ActionType.Create,
            EntityName = "UnitOfMeasure",
            EntityId = entity.Id.ToString(),
            Details = $"Created UoM: {code}"
        });

        return entity;
    }

    public async Task<List<MaterialUomConversionDto>> GetMaterialConversionsAsync(string materialCode, Guid tenantId)
    {
        var matCode = materialCode.ToUpperInvariant();
        return await _db.MaterialUomConversions
            .Where(c => c.TenantId == tenantId && c.MaterialCode == matCode && c.IsActive)
            .OrderBy(c => c.SourceUomCode)
            .Select(c => new MaterialUomConversionDto
            {
                Id = c.Id,
                MaterialCode = c.MaterialCode,
                SourceUomCode = c.SourceUomCode,
                TargetUomCode = c.TargetUomCode,
                ConversionFactor = c.ConversionFactor,
                DensityFactor = c.DensityFactor,
                PlantCode = c.PlantCode,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public UomValidationResult ValidateDecimalPrecision(string uomCode, decimal value)
    {
        var code = uomCode.ToUpperInvariant();
        var uom = _db.UnitsOfMeasure
            .FirstOrDefault(u => u.Msehi == code && u.IsActive);

        if (uom == null)
        {
            return new UomValidationResult
            {
                IsValid = false,
                RoundedValue = value,
                Message = $"UoM '{code}' not found or inactive"
            };
        }

        var rounded = Math.Round(value, uom.Decimals);
        if (rounded != value)
        {
            return new UomValidationResult
            {
                IsValid = false,
                RoundedValue = rounded,
                Message = $"Value {value} exceeds {uom.Decimals} decimal places for UoM '{code}'. Rounded to {rounded}"
            };
        }

        return new UomValidationResult
        {
            IsValid = true,
            RoundedValue = rounded,
            Message = "Value is valid"
        };
    }
}
