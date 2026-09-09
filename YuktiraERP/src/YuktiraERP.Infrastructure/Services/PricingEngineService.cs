using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class PricingEngineService : IPricingEngineService
{
    private readonly YuktiraDbContext _db;

    public PricingEngineService(YuktiraDbContext db) => _db = db;

    public async Task<PricingResult> CalculateItemPricingAsync(PricingContext context, Guid tenantId)
    {
        var result = new PricingResult { Currency = context.Currency, Success = true };

        var conditions = await _db.PricingConditions
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync();

        if (!conditions.Any())
        {
            result.Success = false;
            result.Errors.Add("No active pricing conditions found for this tenant.");
            return result;
        }

        var basePriceCondition = conditions.FirstOrDefault(c => c.Category == "BasePrice");
        if (basePriceCondition == null && context.BaseUnitPrice <= 0)
        {
            result.Success = false;
            result.Errors.Add("No base price condition found and no unit price provided.");
            return result;
        }

        var lineResult = new PricingLineResult
        {
            MaterialCode = context.MaterialCode,
            MaterialName = context.MaterialName,
            Quantity = context.Quantity,
            UOM = context.UOM
        };

        decimal runningSubTotal = 0m;
        int stepNum = 0;

        decimal basePrice = context.BaseUnitPrice > 0
            ? context.BaseUnitPrice
            : EvaluateConditionAmount(basePriceCondition, context.Quantity);

        lineResult.BaseAmount = Math.Round(basePrice * context.Quantity, 2);
        runningSubTotal = lineResult.BaseAmount;

        var stepOutput = new PricingStepOutput
        {
            SequenceNumber = ++stepNum,
            ConditionType = "BasePrice",
            ConditionName = "Base Price",
            Category = PricingConditionType.BasePrice,
            Rate = basePrice,
            Quantity = context.Quantity,
            SubTotal = runningSubTotal,
            StepAmount = lineResult.BaseAmount,
            IsPercentage = false,
            Currency = context.Currency
        };
        lineResult.Steps.Add(stepOutput);

        foreach (var condition in conditions.Where(c => c.Category != "BasePrice").OrderBy(c => c.SequenceNumber))
        {
            var cat = ParseCategory(condition.Category);
            if (cat == PricingConditionType.BasePrice) continue;

            decimal stepAmount = 0m;

            if (condition.IsPercentage && condition.PercentageValue.HasValue)
            {
                stepAmount = Math.Round(runningSubTotal * condition.PercentageValue.Value / 100m, 2);
            }
            else
            {
                stepAmount = EvaluateConditionAmount(condition, context.Quantity);
            }

            switch (cat)
            {
                case PricingConditionType.Discount:
                    lineResult.TotalDiscount += stepAmount;
                    runningSubTotal -= stepAmount;
                    break;
                case PricingConditionType.Surcharge:
                    lineResult.TotalSurcharge += stepAmount;
                    runningSubTotal += stepAmount;
                    break;
                case PricingConditionType.Freight:
                    lineResult.FreightAmount += stepAmount;
                    runningSubTotal += stepAmount;
                    break;
                case PricingConditionType.Tax:
                    lineResult.TaxAmount += stepAmount;
                    break;
                case PricingConditionType.Deduction:
                    runningSubTotal -= stepAmount;
                    break;
            }

            stepNum++;
            lineResult.Steps.Add(new PricingStepOutput
            {
                SequenceNumber = stepNum,
                ConditionType = condition.ConditionType,
                ConditionName = condition.Name,
                Category = cat,
                Rate = condition.Rate,
                Quantity = context.Quantity,
                SubTotal = runningSubTotal,
                StepAmount = stepAmount,
                IsPercentage = condition.IsPercentage,
                Currency = condition.Currency
            });
        }

        if (string.IsNullOrEmpty(context.TaxCode) && lineResult.TaxAmount == 0 && context.Quantity > 0)
        {
            var taxRate = await GetDefaultTaxRateAsync(tenantId);
            if (taxRate > 0)
            {
                var taxAmount = Math.Round(runningSubTotal * taxRate / 100m, 2);
                lineResult.TaxAmount = taxAmount;
                stepNum++;
                lineResult.Steps.Add(new PricingStepOutput
                {
                    SequenceNumber = stepNum,
                    ConditionType = "TAX",
                    ConditionName = $"Output Tax ({taxRate}%)",
                    Category = PricingConditionType.Tax,
                    Rate = taxRate,
                    Quantity = context.Quantity,
                    SubTotal = runningSubTotal,
                    StepAmount = taxAmount,
                    IsPercentage = true,
                    Currency = context.Currency
                });
            }
        }

        lineResult.NetAmount = Math.Round(runningSubTotal, 2);
        lineResult.GrossAmount = Math.Round(runningSubTotal + lineResult.TaxAmount, 2);

        result.LineResult = lineResult;
        result.TotalBaseAmount = lineResult.BaseAmount;
        result.TotalDiscountAmount = lineResult.TotalDiscount;
        result.TotalFreightAmount = lineResult.FreightAmount;
        result.TotalTaxAmount = lineResult.TaxAmount;
        result.TotalNetAmount = lineResult.NetAmount;
        result.TotalGrossAmount = lineResult.GrossAmount;

        return result;
    }

    public async Task<List<PricingConditionDto>> GetActiveConditionsAsync(Guid tenantId)
    {
        return await _db.PricingConditions
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SequenceNumber)
            .Select(c => new PricingConditionDto
            {
                Id = c.Id,
                TenantId = c.TenantId,
                ConditionType = c.ConditionType,
                Name = c.Name,
                Category = ParseCategory(c.Category),
                CalculationType = c.CalculationType,
                Rate = c.Rate,
                Amount = c.Amount,
                PerUnit = c.PerUnit,
                Currency = c.Currency,
                IsPercentage = c.IsPercentage,
                PercentageValue = c.PercentageValue,
                SequenceNumber = c.SequenceNumber,
                IsActive = c.IsActive,
                ValidFrom = c.ValidFrom,
                ValidTo = c.ValidTo,
                CustomerCode = c.CustomerCode,
                MaterialGroup = c.MaterialGroup,
                Plant = c.Plant,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<PricingConditionDto> CreateConditionAsync(Guid tenantId, PricingConditionDto condition)
    {
        var entity = new PricingConditionEntity
        {
            TenantId = tenantId,
            ConditionType = condition.ConditionType,
            Name = condition.Name,
            Category = condition.Category.ToString(),
            CalculationType = condition.CalculationType,
            Rate = condition.Rate,
            Amount = condition.Amount,
            PerUnit = condition.PerUnit,
            Currency = condition.Currency,
            IsPercentage = condition.IsPercentage,
            PercentageValue = condition.PercentageValue,
            SequenceNumber = condition.SequenceNumber,
            IsActive = condition.IsActive,
            ValidFrom = condition.ValidFrom,
            ValidTo = condition.ValidTo,
            CustomerCode = condition.CustomerCode,
            MaterialGroup = condition.MaterialGroup,
            Plant = condition.Plant,
            Description = condition.Description
        };

        _db.PricingConditions.Add(entity);
        await _db.SaveChangesAsync();

        condition.Id = entity.Id;
        condition.TenantId = tenantId;
        return condition;
    }

    private static decimal EvaluateConditionAmount(PricingConditionEntity condition, decimal quantity)
    {
        if (condition.IsPercentage && condition.PercentageValue.HasValue)
            return 0m;

        if (condition.Amount > 0)
            return condition.Amount;

        if (condition.Rate > 0 && condition.PerUnit > 0)
            return Math.Round(condition.Rate * (quantity / condition.PerUnit), 2);

        return condition.Rate;
    }

    private static PricingConditionType ParseCategory(string category) => category switch
    {
        "BasePrice" => PricingConditionType.BasePrice,
        "Discount" => PricingConditionType.Discount,
        "Surcharge" => PricingConditionType.Surcharge,
        "Freight" => PricingConditionType.Freight,
        "Tax" => PricingConditionType.Tax,
        "Deduction" => PricingConditionType.Deduction,
        _ => PricingConditionType.BasePrice
    };

    private async Task<decimal> GetDefaultTaxRateAsync(Guid tenantId)
    {
        var taxCode = await _db.TaxCodes
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsActive && t.TaxType == "GST");
        return taxCode?.Rate ?? 0m;
    }
}
