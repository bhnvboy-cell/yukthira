using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class BankService : IBankService
{
    private readonly YuktiraDbContext _db;

    public BankService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<BankStatement> ImportOfxStatementAsync(Stream file)
    {
        using var reader = new StreamReader(file);
        var content = await reader.ReadToEndAsync();
        var doc = XDocument.Parse(content);
        var ns = XNamespace.None;

        var stmtTrnList = doc.Descendants(ns + "STMTTRN");
        var lines = new List<BankStatementLine>();
        decimal totalDebits = 0, totalCredits = 0;

        foreach (var trn in stmtTrnList)
        {
            var type = trn.Element(ns + "TRNTYPE")?.Value ?? "";
            var amount = decimal.Parse(trn.Element(ns + "TRNAMT")?.Value ?? "0", CultureInfo.InvariantCulture);
            var date = DateTime.Parse(trn.Element(name: "DTPOSTED")?.Value ?? DateTime.UtcNow.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
            var fitId = trn.Element(ns + "FITID")?.Value ?? "";
            var name = trn.Element(ns + "NAME")?.Value ?? "";
            var memo = trn.Element(ns + "MEMO")?.Value ?? "";

            var line = new BankStatementLine
            {
                TransactionDate = date,
                Description = $"{name} {memo}".Trim(),
                Reference = fitId,
                Debit = type == "DEBIT" ? Math.Abs(amount) : 0,
                Credit = type == "CREDIT" ? Math.Abs(amount) : 0,
                Balance = 0,
                Status = "UNMATCHED"
            };

            totalDebits += line.Debit;
            totalCredits += line.Credit;
            lines.Add(line);
        }

        var statement = new BankStatement
        {
            StatementNumber = $"OFX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
            StatementDate = DateTime.UtcNow,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            Status = "PENDING",
            Lines = lines
        };

        return statement;
    }

    public async Task<BankStatement> ImportMt940StatementAsync(string content)
    {
        var lines = new List<BankStatementLine>();
        decimal totalDebits = 0, totalCredits = 0;
        decimal balance = 0;

        var stmtLines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in stmtLines)
        {
            if (line.StartsWith(":61:"))
            {
                var data = line[4..];
                if (data.Length >= 10)
                {
                    var dateStr = data[..6];
                    var year = int.Parse("20" + dateStr[..2]);
                    var month = int.Parse(dateStr[2..4]);
                    var day = int.Parse(dateStr[4..6]);
                    var date = new DateTime(year, month, day);

                    var amountStr = data[10..].Trim();
                    if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        var isDebit = amount < 0;
                        var stmtLine = new BankStatementLine
                        {
                            TransactionDate = date,
                            Description = data,
                            Reference = line,
                            Debit = isDebit ? Math.Abs(amount) : 0,
                            Credit = isDebit ? 0 : amount,
                            Balance = balance,
                            Status = "UNMATCHED"
                        };

                        totalDebits += stmtLine.Debit;
                        totalCredits += stmtLine.Credit;
                        balance += isDebit ? -Math.Abs(amount) : amount;
                        lines.Add(stmtLine);
                    }
                }
            }
        }

        var statement = new BankStatement
        {
            StatementNumber = $"MT940-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
            StatementDate = DateTime.UtcNow,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            Status = "PENDING",
            Lines = lines
        };

        return statement;
    }

    public async Task<BankStatement> ImportCsvStatementAsync(Stream file)
    {
        using var reader = new StreamReader(file);
        var lines = new List<BankStatementLine>();
        decimal totalDebits = 0, totalCredits = 0;
        decimal balance = 0;

        string? headerLine = await reader.ReadLineAsync();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length >= 4)
            {
                var date = DateTime.Parse(parts[0].Trim('"'));
                var description = parts[1].Trim('"');
                var debitStr = parts[2].Trim('"');
                var creditStr = parts[3].Trim('"');

                decimal.TryParse(debitStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var debit);
                decimal.TryParse(creditStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var credit);

                balance += credit - debit;
                totalDebits += debit;
                totalCredits += credit;

                lines.Add(new BankStatementLine
                {
                    TransactionDate = date,
                    Description = description,
                    Debit = debit,
                    Credit = credit,
                    Balance = balance,
                    Status = "UNMATCHED"
                });
            }
        }

        var statement = new BankStatement
        {
            StatementNumber = $"CSV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
            StatementDate = DateTime.UtcNow,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            Status = "PENDING",
            Lines = lines
        };

        return statement;
    }

    public async Task<MatchResult> AutoMatchAsync(Guid statementId)
    {
        var statement = await _db.Set<BankStatementEntity>().FindAsync(statementId);
        if (statement == null)
            return new MatchResult { TotalTransactions = 0 };

        var lines = await _db.Set<BankStatementLineEntity>()
            .Where(l => l.StatementId == statementId && l.Status == "UNMATCHED")
            .ToListAsync();

        var payments = await _db.Payments
            .Where(p => p.Status == "Posted")
            .ToListAsync();

        var matchedPairs = new List<MatchedPair>();
        int matchedCount = 0;

        foreach (var line in lines)
        {
            foreach (var payment in payments)
            {
                var amountMatch = Math.Abs(line.Credit - payment.Amount) < 0.01m ||
                                  Math.Abs(line.Debit - payment.Amount) < 0.01m;

                var dateMatch = Math.Abs((line.TransactionDate - payment.Date).TotalDays) <= 3;

                if (amountMatch && dateMatch)
                {
                    line.Status = "MATCHED";
                    line.MatchedPaymentId = payment.Id;
                    matchedPairs.Add(new MatchedPair
                    {
                        StatementLineId = line.Id,
                        PaymentId = payment.Id,
                        Amount = payment.Amount,
                        Confidence = dateMatch ? 0.95m : 0.7m
                    });
                    matchedCount++;
                    break;
                }
            }
        }

        await _db.SaveChangesAsync();

        return new MatchResult
        {
            TotalTransactions = lines.Count,
            MatchedCount = matchedCount,
            UnmatchedCount = lines.Count - matchedCount,
            MatchedPairs = matchedPairs
        };
    }

    public async Task<Reconciliation> ReconcileAsync(Guid accountId, Guid statementId, List<Guid> matchedIds)
    {
        var statement = await _db.Set<BankStatementEntity>().FindAsync(statementId);
        var account = await _db.Accounts.FindAsync(accountId);

        var matchedLines = await _db.Set<BankStatementLineEntity>()
            .Where(l => matchedIds.Contains(l.Id) && l.StatementId == statementId)
            .ToListAsync();

        var totalMatched = matchedLines.Sum(l => l.Credit - l.Debit);

        var reconciliation = new BankReconciliationEntity
        {
            TenantId = statement?.TenantId ?? Guid.Empty,
            AccountCode = account?.AccountCode ?? "",
            AccountName = account?.AccountName ?? "",
            StatementDate = statement?.StatementDate ?? DateTime.UtcNow,
            StatementBalance = statement?.TotalCredits - statement?.TotalDebits ?? 0,
            LedgerBalance = account?.Balance ?? 0,
            Difference = 0,
            Status = "Completed",
            Notes = $"Reconciled {matchedIds.Count} transactions"
        };

        _db.BankReconciliations.Add(reconciliation);
        await _db.SaveChangesAsync();

        return new Reconciliation
        {
            Id = reconciliation.Id,
            AccountId = accountId,
            ReconciledDate = DateTime.UtcNow,
            OpeningBalance = account?.Balance ?? 0,
            ClosingBalance = reconciliation.StatementBalance,
            TotalMatched = totalMatched,
            TotalUnmatched = reconciliation.StatementBalance - totalMatched,
            Status = "COMPLETED"
        };
    }

    public async Task<List<BankStatementLine>> GetUnmatchedTransactionsAsync(Guid accountId)
    {
        var statements = await _db.Set<BankStatementEntity>()
            .Where(s => s.AccountId == accountId)
            .Select(s => s.Id)
            .ToListAsync();

        var lines = await _db.Set<BankStatementLineEntity>()
            .Where(l => statements.Contains(l.StatementId) && l.Status == "UNMATCHED")
            .Select(l => new BankStatementLine
            {
                Id = l.Id,
                TransactionDate = l.TransactionDate,
                ValueDate = l.ValueDate,
                Description = l.Description,
                Reference = l.Reference,
                Debit = l.Debit,
                Credit = l.Credit,
                Balance = l.Balance,
                Status = l.Status
            })
            .ToListAsync();

        return lines;
    }
}
