namespace YuktiraERP.Core.Domain.Transaction;

public enum TransactionStatus { Active, Inactive, Deprecated }
public enum TransactionGroup { MasterData, Transactions, Reports, Analytics, Administration, Utilities }
public enum ExecutionStatus { Success, Failed, Unauthorized, NotFound }
