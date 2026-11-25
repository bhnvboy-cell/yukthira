using System;
using System.Collections.Generic;
using System.Linq;
using YuktiraERP.Core.Domain.MasterData;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class UomRuleConversionService : IUomRuleConversionService
{
    public decimal ConvertQuantity(
        decimal quantity,
        string fromUomCode,
        string toUomCode,
        Guid? itemId = null,
        IEnumerable<UomConversion>? conversions = null)
    {
        if (string.Equals(fromUomCode, toUomCode, StringComparison.OrdinalIgnoreCase))
            return quantity;

        if (conversions == null)
            throw new ArgumentNullException(nameof(conversions), "Conversion definitions must be provided.");

        var convList = conversions.Where(c => c.IsActive).ToList();

        // 1. If itemId is provided, check for item-specific exact match first
        if (itemId.HasValue)
        {
            var itemDirect = convList.FirstOrDefault(c =>
                c.ItemId == itemId &&
                string.Equals(c.FromUomCode, fromUomCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.ToUomCode, toUomCode, StringComparison.OrdinalIgnoreCase));
            if (itemDirect != null)
                return Math.Round(quantity * itemDirect.Multiplier, 6, MidpointRounding.AwayFromZero);

            var itemReverse = convList.FirstOrDefault(c =>
                c.ItemId == itemId &&
                string.Equals(c.FromUomCode, toUomCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.ToUomCode, fromUomCode, StringComparison.OrdinalIgnoreCase));
            if (itemReverse != null)
            {
                if (itemReverse.Multiplier == 0)
                    throw new InvalidOperationException($"Invalid zero multiplier converting from {toUomCode} to {fromUomCode}.");
                return Math.Round(quantity / itemReverse.Multiplier, 6, MidpointRounding.AwayFromZero);
            }
        }

        // 2. Global direct match
        var globalDirect = convList.FirstOrDefault(c =>
            !c.ItemId.HasValue &&
            string.Equals(c.FromUomCode, fromUomCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ToUomCode, toUomCode, StringComparison.OrdinalIgnoreCase));
        if (globalDirect != null)
            return Math.Round(quantity * globalDirect.Multiplier, 6, MidpointRounding.AwayFromZero);

        // 3. Global reverse match
        var globalReverse = convList.FirstOrDefault(c =>
            !c.ItemId.HasValue &&
            string.Equals(c.FromUomCode, toUomCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ToUomCode, fromUomCode, StringComparison.OrdinalIgnoreCase));
        if (globalReverse != null)
        {
            if (globalReverse.Multiplier == 0)
                throw new InvalidOperationException($"Invalid zero multiplier converting from {toUomCode} to {fromUomCode}.");
            return Math.Round(quantity / globalReverse.Multiplier, 6, MidpointRounding.AwayFromZero);
        }

        // 4. Two-hop conversion via a common intermediate UOM (e.g. Box -> PCS -> Carton)
        var fromHops = convList.Where(c => string.Equals(c.FromUomCode, fromUomCode, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var hop1 in fromHops)
        {
            var hop2 = convList.FirstOrDefault(c =>
                string.Equals(c.FromUomCode, hop1.ToUomCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.ToUomCode, toUomCode, StringComparison.OrdinalIgnoreCase));

            if (hop2 != null)
            {
                decimal intermediate = quantity * hop1.Multiplier;
                return Math.Round(intermediate * hop2.Multiplier, 6, MidpointRounding.AwayFromZero);
            }
        }

        throw new InvalidOperationException($"No UOM conversion path found between '{fromUomCode}' and '{toUomCode}'" +
                                            (itemId.HasValue ? $" for Item '{itemId}'." : "."));
    }
}
