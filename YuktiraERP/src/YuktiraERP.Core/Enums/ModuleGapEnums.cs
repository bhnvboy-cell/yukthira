using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Enums;

// ═══ SD Credit Management ═══
public enum CreditCheckResult
{
    Passed = 0,
    Warning = 1,
    Blocked = 2,
    Exceeded = 3
}

// ═══ SD Scheduling Agreement ═══
public enum ScheduleLineStatus
{
    Open = 0,
    Confirmed = 1,
    PartiallyDelivered = 2,
    Delivered = 3,
    Cancelled = 4
}

// ═══ SD Revenue Recognition ═══
public enum RevenueRecognitionStatus
{
    Pending = 0,
    PartiallyRecognized = 1,
    Recognized = 2,
    Deferred = 3,
    Reversed = 4
}

// ═══ PP Capacity Planning ═══
public enum CapacityCategory
{
    Machine = 0,
    Labor = 1,
    Tool = 2,
    Room = 3
}

public enum CapacityLoadStatus
{
    Underloaded = 0,
    Optimal = 1,
    Overloaded = 2,
    Critical = 3
}

// ═══ PP Kanban ═══
public enum KanbanStatus
{
    Empty = 0,
    Full = 1,
    InProcess = 2,
    Blocked = 3
}

// ═══ WM Putaway ═══
public enum PutawayStrategy
{
    FixedBin = 0,
    OpenStorage = 1,
    NearPickArea = 2,
    ABCClassification = 3,
    RandomStorage = 4,
    Consolidation = 5
}

// ═══ FI Intercompany ═══
public enum IntercompanyTransactionType
{
    IntercompanySale = 0,
    IntercompanyPurchase = 1,
    IntercompanyService = 2,
    IntercompanyLoan = 3,
    IntercompanyDividend = 4
}

// ═══ FI Withholding Tax ═══
public enum WithholdingTaxType
{
    TaxCertificate = 0,
    FinalTax = 1,
    ProgressiveTax = 2
}

// ═══ CO Product Costing ═══
public enum CostingType
{
    Standard = 0,
    MovingAverage = 1,
    Actual = 2,
    Planned = 3
}

public enum CostComponentCategory
{
    MaterialCost = 0,
    LaborCost = 1,
    OverheadCost = 2,
    SubcontractingCost = 3,
    ProcessCost = 4
}

// ═══ CO CO-PA ═══
public enum PaValueType
{
    Revenue = 0,
    CostOfGoodsSold = 1,
    GrossMargin = 2,
    OperatingExpense = 3,
    OperatingProfit = 4,
    NetProfit = 5
}

// ═══ HR Benefits ═══
public enum BenefitType
{
    HealthInsurance = 0,
    DentalInsurance = 1,
    VisionInsurance = 2,
    LifeInsurance = 3,
    RetirementPlan = 4,
    PaidTimeOff = 5,
    StockOption = 6,
    Other = 99
}

// ═══ HR Succession ═══
public enum SuccessionReadiness
{
    ReadyNow = 0,
    ReadyIn1Year = 1,
    ReadyIn2Years = 2,
    ReadyIn3PlusYears = 3,
    NotReady = 4
}

// ═══ Financial Close ═══
public enum ClosePeriodStatus
{
    Open = 0,
    ClosingInProgress = 1,
    Closed = 2,
    Reopened = 3
}
