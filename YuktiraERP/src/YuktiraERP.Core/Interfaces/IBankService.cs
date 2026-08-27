namespace YuktiraERP.Core.Interfaces;

public interface IBankService
{
    Task<BankStatement> ImportOfxStatementAsync(Stream file);
    Task<BankStatement> ImportMt940StatementAsync(string content);
    Task<BankStatement> ImportCsvStatementAsync(Stream file);
    Task<MatchResult> AutoMatchAsync(Guid statementId);
    Task<Reconciliation> ReconcileAsync(Guid accountId, Guid statementId, List<Guid> matchedIds);
    Task<List<BankStatementLine>> GetUnmatchedTransactionsAsync(Guid accountId);
}

public class BankStatement
{
    public Guid Id { get; set; }
    public string StatementNumber { get; set; } = "";
    public DateTime StatementDate { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public string Status { get; set; } = "PENDING";
    public List<BankStatementLine> Lines { get; set; } = new();
}

public class BankStatementLine
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public string Description { get; set; } = "";
    public string Reference { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "UNMATCHED";
}

public class MatchResult
{
    public int TotalTransactions { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public List<MatchedPair> MatchedPairs { get; set; } = new();
}

public class MatchedPair
{
    public Guid StatementLineId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal Confidence { get; set; }
}

public class Reconciliation
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime ReconciledDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalMatched { get; set; }
    public decimal TotalUnmatched { get; set; }
    public string Status { get; set; } = "COMPLETED";
}
