namespace YuktiraERP.Core.Enums;

public enum InspectionLotOrigin
{
    GoodsReceipt = 1,
    ProductionOrder = 4,
    InboundDelivery = 5,
    StockTransfer = 8,
    Manual = 9
}

public enum InspectionType
{
    QualityInspection = 1,
    SkipLot = 2,
    StockSampling = 3,
    ControlInspection = 4
}

public enum UsageDecisionCode
{
    Accepted,
    Rejected,
    Rework,
    Scrap,
    Restricted,
    Reversed
}

public enum QualityScoreMethod
{
    SingleFigure,
    WeightedAverage,
    PercentageDefective,
    AttributeBased
}

public enum NonConformanceSeverity
{
    Minor,
    Major,
    Critical
}

public enum NonConformanceStatus
{
    Open,
    InProgress,
    Contained,
    CorrectiveAction,
    PreventiveAction,
    Verified,
    Closed,
    Rejected
}

public enum CAPAType
{
    Corrective,
    Preventive,
    Improvement
}

public enum CAPAStatus
{
    Draft,
    Open,
    InProgress,
    Overdue,
    Completed,
    Verified,
    Closed
}

public enum HandlingUnitStatus
{
    Created,
    Packed,
    Weighed,
    Released,
    Shipped,
    Cancelled
}

public enum LabCalculationType
{
    MoistureContent,
    DrySubstance,
    StarchPurity,
    GrainDefect,
    BaumeGravity,
    DEValue,
    pHValue,
    Viscosity,
    AshContent,
    ProteinContent
}

public enum CertificateStatus
{
    Draft,
    Issued,
    Sent,
    Expired,
    Revoked
}
