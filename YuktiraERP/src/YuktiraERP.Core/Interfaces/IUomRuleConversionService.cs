using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.MasterData;

namespace YuktiraERP.Core.Interfaces;

public interface IUomRuleConversionService
{
    decimal ConvertQuantity(
        decimal quantity,
        string fromUomCode,
        string toUomCode,
        Guid? itemId = null,
        IEnumerable<UomConversion>? conversions = null);
}
