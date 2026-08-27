using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class BankServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private static Stream ToStream(string content)
    {
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ImportOfxStatementAsync_ParsesTransactions()
    {
        var db = CreateDb();
        var service = new BankService(db);
        var ofx = @"<?xml version=""1.0""?>
<OFX>
<BANKTRANLIST>
<STMTTRN><TRNTYPE>DEBIT</TRNTYPE><TRNAMT>-150.00</TRNAMT><DTPOSTED>2026-08-15</DTPOSTED><FITID>TXN001</FITID><NAME>Vendor Payment</NAME><MEMO>Invoice 123</MEMO></STMTTRN>
<STMTTRN><TRNTYPE>CREDIT</TRNTYPE><TRNAMT>500.00</TRNAMT><DTPOSTED>2026-08-16</DTPOSTED><FITID>TXN002</FITID><NAME>Customer Payment</NAME><MEMO>PO 456</MEMO></STMTTRN>
</BANKTRANLIST>
</OFX>";

        var stream = ToStream(ofx);
        var statement = await service.ImportOfxStatementAsync(stream);

        Assert.NotNull(statement);
        Assert.StartsWith("OFX-", statement.StatementNumber);
        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(150, statement.TotalDebits);
        Assert.Equal(500, statement.TotalCredits);
        Assert.Equal("PENDING", statement.Status);
    }

    [Fact]
    public async Task ImportMt940StatementAsync_ParsesTransactions()
    {
        var db = CreateDb();
        var service = new BankService(db);
        // MT940 :61: format: date(6) + valuedate(4) + amount
        var mt940 = @":20:REF123
:25:ACCOUNT/123456
:60F:C1000.00EUR
:61:2608150815 500.00
:61:2608160816-150.00
:62F:C1350.00EUR";

        var statement = await service.ImportMt940StatementAsync(mt940);

        Assert.NotNull(statement);
        Assert.StartsWith("MT940-", statement.StatementNumber);
        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(150, statement.TotalDebits);
        Assert.Equal(500, statement.TotalCredits);
    }

    [Fact]
    public async Task ImportCsvStatementAsync_ParsesTransactions()
    {
        var db = CreateDb();
        var service = new BankService(db);
        var csv = "Date,Description,Debit,Credit\n2026-08-15,Supplier Invoice,250.00,0.00\n2026-08-16,Customer Payment,0.00,1000.00\n";

        var stream = ToStream(csv);
        var statement = await service.ImportCsvStatementAsync(stream);

        Assert.NotNull(statement);
        Assert.StartsWith("CSV-", statement.StatementNumber);
        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(250, statement.TotalDebits);
        Assert.Equal(1000, statement.TotalCredits);
    }

    [Fact]
    public async Task ImportCsvStatementAsync_EmptyFile_ReturnsEmptyStatement()
    {
        var db = CreateDb();
        var service = new BankService(db);
        var csv = "Date,Description,Debit,Credit\n";

        var stream = ToStream(csv);
        var statement = await service.ImportCsvStatementAsync(stream);

        Assert.NotNull(statement);
        Assert.Empty(statement.Lines);
        Assert.Equal(0, statement.TotalDebits);
    }

    [Fact]
    public async Task AutoMatchAsync_NoStatement_ReturnsZeroTotal()
    {
        var db = CreateDb();
        var service = new BankService(db);

        var result = await service.AutoMatchAsync(Guid.NewGuid());

        Assert.Equal(0, result.TotalTransactions);
    }

    [Fact]
    public async Task ReconcileAsync_CreatesReconciliation()
    {
        var db = CreateDb();
        var account = new AccountEntity
        {
            AccountCode = "1000", AccountName = "Cash", Type = "Asset",
            Balance = 5000, IsActive = true
        };
        db.Accounts.Add(account);

        var stmtEntity = new BankStatementEntity
        {
            StatementNumber = "STMT-001", AccountId = account.Id,
            StatementDate = DateTime.UtcNow, TotalDebits = 200,
            TotalCredits = 1000, Status = "PENDING"
        };
        db.BankStatements.Add(stmtEntity);
        await db.SaveChangesAsync();

        var service = new BankService(db);
        var result = await service.ReconcileAsync(account.Id, stmtEntity.Id, new());

        Assert.Equal("COMPLETED", result.Status);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task GetUnmatchedTransactionsAsync_ReturnsUnmatchedLines()
    {
        var db = CreateDb();
        var account = new AccountEntity
        {
            AccountCode = "2000", AccountName = "Bank", Type = "Asset",
            Balance = 10000, IsActive = true
        };
        db.Accounts.Add(account);

        var stmt = new BankStatementEntity
        {
            StatementNumber = "STMT-002", AccountId = account.Id,
            StatementDate = DateTime.UtcNow, TotalDebits = 100,
            TotalCredits = 500, Status = "PENDING"
        };
        db.BankStatements.Add(stmt);
        await db.SaveChangesAsync();

        db.BankStatementLines.Add(new BankStatementLineEntity
        {
            StatementId = stmt.Id, TransactionDate = DateTime.UtcNow,
            Description = "Unmatched txn", Debit = 100, Status = "UNMATCHED"
        });
        db.BankStatementLines.Add(new BankStatementLineEntity
        {
            StatementId = stmt.Id, TransactionDate = DateTime.UtcNow,
            Description = "Matched txn", Credit = 200, Status = "MATCHED"
        });
        await db.SaveChangesAsync();

        var service = new BankService(db);
        var unmatched = await service.GetUnmatchedTransactionsAsync(account.Id);

        Assert.Single(unmatched);
        Assert.Equal("Unmatched txn", unmatched[0].Description);
    }

    [Fact]
    public async Task AutoMatchAsync_WithPayments_MatchesByAmount()
    {
        var db = CreateDb();
        var account = new AccountEntity
        {
            AccountCode = "3000", AccountName = "Main", Type = "Asset",
            Balance = 20000, IsActive = true
        };
        db.Accounts.Add(account);

        var stmt = new BankStatementEntity
        {
            StatementNumber = "STMT-003", AccountId = account.Id,
            StatementDate = DateTime.UtcNow, TotalDebits = 0,
            TotalCredits = 500, Status = "PENDING"
        };
        db.BankStatements.Add(stmt);
        await db.SaveChangesAsync();

        db.BankStatementLines.Add(new BankStatementLineEntity
        {
            StatementId = stmt.Id, TransactionDate = DateTime.UtcNow,
            Description = "Payment received", Credit = 500, Status = "UNMATCHED"
        });

        var payment = new PaymentEntity
        {
            PaymentNumber = "PAY-001", Date = DateTime.UtcNow,
            PartyName = "Customer A", Amount = 500, Status = "Posted"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new BankService(db);
        var result = await service.AutoMatchAsync(stmt.Id);

        Assert.Equal(1, result.TotalTransactions);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(0, result.UnmatchedCount);
        Assert.Single(result.MatchedPairs);
    }
}
