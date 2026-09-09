namespace YuktiraERP.Core.Enums;

public enum MbMovementType
{
    GoodsReceiptForPO = 101,
    ReversalOfGRForPO = 102,
    GRIntoGRBlockedStock = 103,
    GoodsIssueForCostCenter = 201,
    ReversalOfGIFromCostCenter = 202,
    GoodsIssueForProductionOrder = 261,
    ReversalOfGIFromProductionOrder = 262,
    TransferPlantToPlant = 301,
    ReversalOfTransferPlantToPlant = 302,
    TransferStorageLocationToStorageLocation = 311,
    ReversalOfTransferSLocToSLoc = 312,
    TransferQIToUnrestricted = 321,
    TransferUnrestrictedToQI = 322,
    ReversalOfTransferQIToUnrestricted = 323,
    ReversalOfTransferUnrestrictedToQI = 324,
    TransferUnrestrictedToBlocked = 343,
    ReversalOfTransferUnrestrictedToBlocked = 344,
    TransferBlockedToUnrestricted = 349,
    TransferConsignmentToUnrestricted = 411,
    ReversalOfTransferConsignmentToUnrestricted = 412,
    GIForReturnToVendor = 451,
    ReversalOfGIReturnToVendor = 452,
    GIForSubcontractingOrder = 541,
    ReversalOfGISubcontractingOrder = 542,
    InitialStock = 561,
    ReversalOfInitialStock = 562,
    GoodsReceiptForDelivery = 601,
    ReversalOfGRForDelivery = 602,
    CustomerReturn = 651,
    ReversalOfCustomerReturn = 652
}

public enum StockBucket
{
    Unrestricted,
    QualityInspection,
    Blocked,
    GRBlocked,
    Reserved,
    InTransit,
    Consignment,
    Subcontracting
}

public enum MbDocumentType
{
    MaterialDocument,
    Reservation,
    PhysicalInventory,
    Revaluation
}

public enum MbReservationType
{
    Manual,
    Automatic,
    Backflush,
    Kanban,
    MRP
}

public enum MbStockOverviewLevel
{
    Material,
    Plant,
    StorageLocation,
    Batch
}
