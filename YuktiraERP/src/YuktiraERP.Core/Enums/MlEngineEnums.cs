namespace YuktiraERP.Core.Enums;

public enum VisionDefectType
{
    None = 0,
    SurfaceScratch = 1,
    Dent = 2,
    Discoloration = 3,
    Crack = 4,
    ForeignParticle = 5,
    GrainDefect = 6,
    MoistureDamage = 7,
    LabelMisalignment = 8,
    SealFailure = 9
}

public enum VisionSeverity
{
    Pass = 0,
    Warning = 1,
    MinorDefect = 2,
    MajorDefect = 3,
    CriticalDefect = 4
}

public enum ModelTrainingStatus
{
    Pending = 0,
    Training = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum NlQueryEntityType
{
    Material = 0,
    Vendor = 1,
    Customer = 2,
    PurchaseOrder = 3,
    SalesOrder = 4,
    InspectionLot = 5,
    StockBalance = 6,
    JournalEntry = 7,
    Employee = 8,
    ProductionOrder = 9
}

public enum NlQueryOperation
{
    Select = 0,
    Count = 1,
    Sum = 2,
    Average = 3,
    Filter = 4,
    GroupBy = 5,
    Sort = 6
}
