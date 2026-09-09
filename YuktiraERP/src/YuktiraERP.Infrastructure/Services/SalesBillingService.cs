using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class SalesBillingService : ISalesBillingService
{
    private readonly YuktiraDbContext _db;
    private readonly IPricingEngineService _pricingEngine;

    private const string GL_CUSTOMER_AR = "1400";
    private const string GL_REVENUE = "4000";
    private const string GL_FREIGHT = "4100";
    private const string GL_OUTPUT_TAX = "2300";

    public SalesBillingService(YuktiraDbContext db, IPricingEngineService pricingEngine)
    {
        _db = db;
        _pricingEngine = pricingEngine;
    }

    public async Task<BillingReleaseResult> ReleaseBillingDocumentToFIAsync(Guid billingDocumentId, Guid tenantId)
    {
        var result = new BillingReleaseResult();

        var billingDoc = await _db.BillingDocuments
            .FirstOrDefaultAsync(b => b.Id == billingDocumentId && b.TenantId == tenantId);
        if (billingDoc == null)
        {
            result.Errors.Add($"Billing document {billingDocumentId} not found for tenant {tenantId}.");
            return result;
        }

        if (billingDoc.Status == "Released" || billingDoc.Status == "FI_Posted")
        {
            result.Errors.Add($"Billing document {billingDoc.DocumentNumber} is already released to FI (Status: {billingDoc.Status}).");
            return result;
        }

        var deliveryEntity = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.DeliveryNumber == billingDoc.SoNumber);

        var soNumber = billingDoc.SoNumber;
        var deliveryNumber = deliveryEntity?.DeliveryNumber ?? "";

        var customerName = billingDoc.CustomerName;
        var customerCode = await GetCustomerCodeAsync(customerName, tenantId);

        var pricingContext = new PricingContext
        {
            MaterialCode = "",
            MaterialName = "",
            CustomerCode = customerCode,
            CustomerName = customerName,
            SalesOrderNumber = soNumber,
            DeliveryNumber = deliveryNumber,
            Quantity = 1,
            UOM = "EA",
            BaseUnitPrice = billingDoc.Amount,
            Currency = "INR",
            PricingDate = billingDoc.Date
        };

        var pricingResult = await _pricingEngine.CalculateItemPricingAsync(pricingContext, tenantId);
        if (!pricingResult.Success)
        {
            result.Errors.AddRange(pricingResult.Errors);
            return result;
        }

        var fiDocNumber = GenerateFIDocumentNumber();
        var postingDate = DateTime.UtcNow;
        var period = $"{postingDate:yyyy-MM}";
        var fiscalYear = postingDate.Year.ToString();

        var journalLines = new List<BillingReleaseJournalLine>();
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;

        var arDebit = pricingResult.TotalGrossAmount;
        journalLines.Add(new BillingReleaseJournalLine
        {
            AccountCode = GL_CUSTOMER_AR,
            AccountName = $"AR - {customerName}",
            Description = $"Billing {billingDoc.DocumentNumber} - AR Control",
            DebitAmount = arDebit,
            CreditAmount = 0,
            CostCenter = "",
            ProfitCenter = ""
        });
        totalDebit += arDebit;

        var revenueCredit = pricingResult.TotalBaseAmount;
        journalLines.Add(new BillingReleaseJournalLine
        {
            AccountCode = GL_REVENUE,
            AccountName = "Revenue",
            Description = $"Billing {billingDoc.DocumentNumber} - Revenue",
            DebitAmount = 0,
            CreditAmount = revenueCredit,
            CostCenter = "",
            ProfitCenter = ""
        });
        totalCredit += revenueCredit;

        if (pricingResult.TotalFreightAmount > 0)
        {
            journalLines.Add(new BillingReleaseJournalLine
            {
                AccountCode = GL_FREIGHT,
                AccountName = "Freight Charges",
                Description = $"Billing {billingDoc.DocumentNumber} - Freight",
                DebitAmount = 0,
                CreditAmount = pricingResult.TotalFreightAmount,
                CostCenter = "",
                ProfitCenter = ""
            });
            totalCredit += pricingResult.TotalFreightAmount;
        }

        if (pricingResult.TotalTaxAmount > 0)
        {
            journalLines.Add(new BillingReleaseJournalLine
            {
                AccountCode = GL_OUTPUT_TAX,
                AccountName = "Output Tax Payable",
                Description = $"Billing {billingDoc.DocumentNumber} - VAT/Tax",
                DebitAmount = 0,
                CreditAmount = pricingResult.TotalTaxAmount,
                CostCenter = "",
                ProfitCenter = ""
            });
            totalCredit += pricingResult.TotalTaxAmount;
        }

        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            result.Warnings.Add($"Debit/Credit imbalance: Debit={totalDebit:F2}, Credit={totalCredit:F2}. Difference={Math.Abs(totalDebit - totalCredit):F2}");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var universalJournalLines = new List<UniversalJournalEntity>();
            int lineNum = 1;

            foreach (var jl in journalLines)
            {
                universalJournalLines.Add(new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = postingDate.Year,
                    Period = postingDate.Month,
                    DocumentNumber = fiDocNumber,
                    DocumentType = "FI-BILLING",
                    DocumentDate = billingDoc.Date,
                    PostingDate = postingDate,
                    LineNumber = lineNum++,
                    AccountCode = jl.AccountCode,
                    AccountName = jl.AccountName,
                    AccountType = jl.DebitAmount > 0 ? "DEBIT" : "CREDIT",
                    DebitAmount = jl.DebitAmount,
                    CreditAmount = jl.CreditAmount,
                    Currency = "INR",
                    ExchangeRate = 1.0m,
                    AmountLC = jl.DebitAmount > 0 ? jl.DebitAmount : jl.CreditAmount,
                    CostCenter = jl.CostCenter,
                    ProfitCenter = jl.ProfitCenter,
                    CustomerCode = customerCode,
                    Reference = billingDoc.DocumentNumber,
                    Description = jl.Description,
                    CreatedBy = "SYSTEM",
                    PostedAt = postingDate,
                    Status = "Posted"
                });
            }

            _db.UniversalJournals.AddRange(universalJournalLines);

            var arEntry = new AREntryEntity
            {
                TenantId = tenantId,
                DocumentNumber = fiDocNumber,
                Date = postingDate,
                CustomerName = customerName,
                Amount = pricingResult.TotalGrossAmount,
                ReceivedAmount = 0,
                Status = "Open"
            };
            _db.AREntries.Add(arEntry);

            billingDoc.Status = "FI_Posted";
            billingDoc.UpdatedAt = DateTime.UtcNow;
            _db.BillingDocuments.Update(billingDoc);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.BillingDocumentNumber = billingDoc.DocumentNumber;
            result.FiDocumentNumber = fiDocNumber;
            result.FiDocumentId = arEntry.Id;
            result.TotalNetAmount = pricingResult.TotalNetAmount;
            result.TotalTaxAmount = pricingResult.TotalTaxAmount;
            result.TotalGrossAmount = pricingResult.TotalGrossAmount;
            result.BaseAmount = pricingResult.TotalBaseAmount;
            result.FreightAmount = pricingResult.TotalFreightAmount;
            result.Currency = pricingResult.Currency;
            result.JournalLines = journalLines;
            result.SalesOrderId = soNumber;
            result.DeliveryNoteId = deliveryNumber;
            result.FiJournalId = fiDocNumber;
            result.PostedAt = postingDate;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Failed to post FI document: {ex.Message}");
        }

        return result;
    }

    public async Task<BillingDocumentDetailDto> GetBillingDocumentDetailAsync(Guid billingDocumentId, Guid tenantId)
    {
        var billingDoc = await _db.BillingDocuments
            .FirstOrDefaultAsync(b => b.Id == billingDocumentId && b.TenantId == tenantId);
        if (billingDoc == null)
            return new BillingDocumentDetailDto();

        var lines = await _db.BillingDocumentLines
            .Where(l => l.BillingDocumentId == billingDocumentId)
            .OrderBy(l => l.LineNumber)
            .Select(l => new BillingDocumentLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                MaterialCode = l.MaterialCode,
                MaterialName = l.MaterialName,
                Quantity = l.Quantity,
                UOM = l.UOM,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                Discount = l.Discount,
                NetAmount = l.NetAmount
            })
            .ToListAsync();

        var arEntry = await _db.AREntries
            .FirstOrDefaultAsync(a => a.DocumentNumber == billingDoc.DocumentNumber && a.TenantId == tenantId);

        return new BillingDocumentDetailDto
        {
            Id = billingDoc.Id,
            DocumentNumber = billingDoc.DocumentNumber,
            Date = billingDoc.Date,
            SoNumber = billingDoc.SoNumber,
            CustomerName = billingDoc.CustomerName,
            Amount = billingDoc.Amount,
            Status = billingDoc.Status,
            FiDocumentNumber = arEntry?.DocumentNumber ?? "",
            PostedAt = arEntry?.Date,
            Lines = lines
        };
    }

    private static string GenerateFIDocumentNumber()
    {
        var now = DateTime.UtcNow;
        var seq = $"{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..4]}";
        return $"FB{seq}";
    }

    private async Task<string> GetCustomerCodeAsync(string customerName, Guid tenantId)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Name == customerName);
        return customer?.Code ?? "CUST-UNKNOWN";
    }
}
