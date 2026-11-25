using System;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.MasterData;

public class Company : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TaxRegistrationNumber { get; set; } = string.Empty;
    public string FunctionalCurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}

public class Branch : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
}

public class Currency : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty; // ISO 4217, e.g. USD, EUR, INR
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}

public enum ExchangeRateType
{
    Spot = 1,
    Closing = 2,
    Budget = 3
}

public class CurrencyExchangeRate : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public ExchangeRateType RateType { get; set; } = ExchangeRateType.Spot;
    public decimal Rate { get; set; }
    public DateOnly EffectiveDate { get; set; }
}

public enum ItemValuationMethod
{
    MovingAverage = 1,
    Standard = 2,
    FIFO = 3
}

public enum ItemClassification
{
    RawMaterial = 1,
    Wip = 2,
    FinishedGood = 3,
    TradingGood = 4,
    Service = 5,
    Consumable = 6
}

public class MasterItem : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemClassification Classification { get; set; } = ItemClassification.TradingGood;
    public ItemValuationMethod ValuationMethod { get; set; } = ItemValuationMethod.MovingAverage;
    public string BaseUomCode { get; set; } = string.Empty;
    public string? PurchaseUomCode { get; set; }
    public string? SalesUomCode { get; set; }
    public decimal StandardCost { get; set; } = 0m;
    public bool IsBatchTracked { get; set; } = false;
    public bool IsSerialTracked { get; set; } = false;
    public bool IsSalesItem { get; set; } = true;
    public bool IsPurchaseItem { get; set; } = true;
    public bool IsInventoryItem { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class UomDefinition : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UomCategory { get; set; } = "Quantity";
    public int Precision { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}

public class UomConversion : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ItemId { get; set; }
    public string FromUomCode { get; set; } = string.Empty;
    public string ToUomCode { get; set; } = string.Empty;
    public decimal Multiplier { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Warehouse : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBinActivated { get; set; } = false;
    public bool IsQuarantine { get; set; } = false;
    public bool IsTransit { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public ICollection<WarehouseBin> Bins { get; set; } = new List<WarehouseBin>();
}

public class WarehouseBin : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public bool IsDisabled { get; set; } = false;
}

public enum PartnerType
{
    Customer = 1,
    Vendor = 2,
    Both = 3
}

public class BusinessPartner : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PartnerType Type { get; set; } = PartnerType.Customer;
    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; } = 0m;
    public decimal CurrentBalance { get; set; } = 0m;
    public string PaymentTermsCode { get; set; } = "NET30";
    public string TaxRegistrationNumber { get; set; } = string.Empty;
    public bool IsHold { get; set; } = false;
    public string? HoldReason { get; set; }
    public bool IsActive { get; set; } = true;
}

public class NumberingSeries : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DocumentType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public long NextNumber { get; set; } = 1;
    public long EndingNumber { get; set; } = 99999999;
    public int PaddingLength { get; set; } = 6;
    public int FiscalYear { get; set; } = DateTime.UtcNow.Year;
    public bool IsLocked { get; set; } = false;

    public string GenerateNextDocumentNumber()
    {
        if (NextNumber > EndingNumber)
            throw new InvalidOperationException($"Numbering series for {DocumentType} has exceeded maximum limit {EndingNumber}.");

        string numStr = NextNumber.ToString().PadLeft(PaddingLength, '0');
        string result = $"{Prefix}{numStr}{Suffix}";
        NextNumber++;
        return result;
    }
}
