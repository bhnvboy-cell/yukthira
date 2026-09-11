using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class IntercompanyAccountingService : IIntercompanyAccountingService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<IntercompanyAccountingService> _logger;

    public IntercompanyAccountingService(YuktiraDbContext db, ILogger<IntercompanyAccountingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IntercompanyTransactionResult> PostIntercompanyTransactionAsync(IntercompanyTransactionRequest request)
    {
        try
        {
            var sendingDoc = $"IC-{request.SendingCompanyCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var receivingDoc = $"IC-{request.ReceivingCompanyCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var sendingEntry = new JournalEntryEntity
            {
                DocumentNumber = sendingDoc,
                EntryDate = request.TransactionDate,
                Account = $"IC-Receivable-{request.ReceivingCompanyCode}",
                Debit = request.Amount,
                Credit = 0,
                Reference = $"Intercompany {request.TransactionType}: {request.Description}"
            };

            var receivingEntry = new JournalEntryEntity
            {
                DocumentNumber = receivingDoc,
                EntryDate = request.TransactionDate,
                Account = $"IC-Payable-{request.SendingCompanyCode}",
                Debit = 0,
                Credit = request.Amount,
                Reference = $"Intercompany {request.TransactionType}: {request.Description}"
            };

            _db.JournalEntries.Add(sendingEntry);
            _db.JournalEntries.Add(receivingEntry);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Intercompany transaction posted: Sending={Sending}, Receiving={Receiving}, Amount={Amount}",
                sendingDoc, receivingDoc, request.Amount);

            return new IntercompanyTransactionResult
            {
                Success = true,
                SendingDocumentNumber = sendingDoc,
                ReceivingDocumentNumber = receivingDoc,
                ExchangeRate = 1.0m,
                LocalAmount = request.Amount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Intercompany transaction failed");
            return new IntercompanyTransactionResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<IntercompanyTransactionResult>> ReconcileIntercompanyAsync(string companyCode, int fiscalYear, int period, Guid tenantId)
    {
        return Task.FromResult(new List<IntercompanyTransactionResult>());
    }
}

public class WithholdingTaxService : IWithholdingTaxService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<WithholdingTaxService> _logger;

    private static readonly Dictionary<string, decimal> TaxRates = new()
    {
        ["WHT-10"] = 10m,
        ["WHT-15"] = 15m,
        ["WHT-20"] = 20m,
        ["WHT-30"] = 30m,
    };

    public WithholdingTaxService(YuktiraDbContext db, ILogger<WithholdingTaxService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WithholdingTaxResult> CalculateWithholdingTaxAsync(WithholdingTaxRequest request)
    {
        try
        {
            if (!TaxRates.TryGetValue(request.TaxCode, out var taxRate))
                return new WithholdingTaxResult { Success = false, Errors = { $"Unknown tax code: {request.TaxCode}" } };

            var taxAmount = request.GrossAmount * taxRate / 100m;
            var netAmount = request.GrossAmount - taxAmount;

            var certNumber = request.TaxType == WithholdingTaxType.TaxCertificate
                ? $"WTC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}"
                : null;

            _logger.LogInformation("Withholding tax calculated: Vendor={Vendor}, Gross={Gross}, Tax={Tax}, Net={Net}",
                request.VendorCode, request.GrossAmount, taxAmount, netAmount);

            return new WithholdingTaxResult
            {
                Success = true,
                GrossAmount = request.GrossAmount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                NetAmount = netAmount,
                TaxCertificateNumber = certNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Withholding tax calculation failed");
            return new WithholdingTaxResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<WithholdingTaxResult>> GetWithholdingTaxReportAsync(string companyCode, DateTime from, DateTime to, Guid tenantId)
    {
        return Task.FromResult(new List<WithholdingTaxResult>());
    }
}

public class FinancialCloseService : IFinancialCloseService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<FinancialCloseService> _logger;

    public FinancialCloseService(YuktiraDbContext db, ILogger<FinancialCloseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<FinancialCloseResult> ClosePeriodAsync(FinancialCloseRequest request)
    {
        try
        {
            var openItems = await _db.JournalEntries
                .Where(j => j.EntryDate.Year == request.FiscalYear && j.EntryDate.Month == request.Period)
                .CountAsync();

            var blockingIssues = new List<string>();

            if (openItems > 0 && !request.ForceClose)
            {
                blockingIssues.Add($"{openItems} open journal entries in period {request.Period}");
            }

            if (blockingIssues.Any() && !request.ForceClose)
            {
                return new FinancialCloseResult
                {
                    Success = false,
                    CompanyCode = request.CompanyCode,
                    Status = ClosePeriodStatus.Open,
                    OpenItemsCount = openItems,
                    BlockingIssues = blockingIssues
                };
            }

            _logger.LogInformation("Period closed: Company={Company}, Year={Year}, Period={Period}, Force={Force}",
                request.CompanyCode, request.FiscalYear, request.Period, request.ForceClose);

            return new FinancialCloseResult
            {
                Success = true,
                CompanyCode = request.CompanyCode,
                Status = ClosePeriodStatus.Closed,
                OpenItemsCount = openItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Period close failed");
            return new FinancialCloseResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<FinancialCloseResult> ReopenPeriodAsync(string companyCode, int fiscalYear, int period, Guid tenantId)
    {
        _logger.LogInformation("Period reopened: Company={Company}, Year={Year}, Period={Period}", companyCode, fiscalYear, period);
        return new FinancialCloseResult
        {
            Success = true,
            CompanyCode = companyCode,
            Status = ClosePeriodStatus.Reopened
        };
    }

    public Task<List<FinancialCloseResult>> GetCloseStatusAsync(string companyCode, int fiscalYear, Guid tenantId)
    {
        return Task.FromResult(new List<FinancialCloseResult>());
    }
}
