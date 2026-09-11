namespace YuktiraERP.Core.Enums;

public enum EdiMessageType
{
    PurchaseOrder = 850,
    PurchaseOrderAck = 855,
    Invoice = 810,
    AdvanceShipNotice = 856,
    FunctionalAck = 997,
    MdReceipt = 0
}

public enum EdiTransportProtocol
{
    As2 = 0,
    As4 = 1,
    Ftp = 2,
    Https = 3
}

public enum EdiSecurityLevel
{
    None = 0,
    Sign = 1,
    Encrypt = 2,
    SignAndEncrypt = 3
}

public enum MdnStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2,
    Expired = 3
}

public enum EdiTransactionStatus
{
    Received = 0,
    Processed = 1,
    Rejected = 2,
    Acknowledged = 3,
    PendingTranslation = 4,
    Translated = 5,
    Applied = 6,
    Error = 7
}
