namespace YuktiraERP.Core.Domain.Transaction;

public enum TransactionStatus { Active, Inactive, Deprecated }
public enum TransactionGroup { MasterData, Transactions, Process, Reports, Configuration, Administration, Analytics, Utilities }
public enum ExecutionStatus { Success, Failed, Unauthorized, NotFound }
