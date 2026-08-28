using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class UniversalJournalService : IUniversalJournalService
{
    private readonly YuktiraDbContext _db;

    public UniversalJournalService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<JournalPostResult> PostAsync(JournalPostRequest request)
    {
        var totalDebit = request.LineItems.Sum(l => l.DebitAmount);
        var totalCredit = request.LineItems.Sum(l => l.CreditAmount);

        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            return new JournalPostResult
            {
                Success = false,
                Message = $"Debit/Credit imbalance: Debit={totalDebit}, Credit={totalCredit}"
            };
        }

        if (!request.LineItems.Any())
        {
            return new JournalPostResult
            {
                Success = false,
                Message = "No line items provided"
            };
        }

        var docNumber = GenerateDocumentNumber();
        var now = DateTime.UtcNow;
        int lineNum = 1;

        foreach (var line in request.LineItems)
        {
            var journal = new UniversalJournalEntity
            {
                Id = Guid.NewGuid(),
                FiscalYear = int.TryParse(request.FiscalYear, out var fy) ? fy : now.Year,
                Period = now.Month,
                DocumentNumber = docNumber,
                DocumentType = request.DocumentType,
                DocumentDate = request.DocumentDate,
                PostingDate = request.PostingDate,
                LineNumber = lineNum++,
                AccountCode = line.GlAccount,
                DebitAmount = line.DebitAmount,
                CreditAmount = line.CreditAmount,
                Currency = line.Currency,
                ExchangeRate = 1.0m,
                AmountLC = line.DebitAmount > 0 ? line.DebitAmount : -line.CreditAmount,
                CostCenter = line.CostCenter,
                ProfitCenter = line.ProfitCenter,
                Plant = line.Plant,
                MaterialCode = line.Material,
                Reference = request.Reference,
                Description = line.Description,
                PostedAt = now,
                CreatedBy = "SYSTEM",
                Status = "Posted",
                Hash = ComputeJournalHash(docNumber, lineNum, line.GlAccount, line.DebitAmount, line.CreditAmount)
            };

            _db.UniversalJournals.Add(journal);
        }

        await _db.SaveChangesAsync();

        return new JournalPostResult
        {
            Success = true,
            DocumentNumber = docNumber,
            FiscalYear = request.FiscalYear,
            CompanyCode = request.CompanyCode,
            Message = $"Posted successfully: {request.LineItems.Count} lines, Debit={totalDebit}, Credit={totalCredit}"
        };
    }

    public async Task<JournalBatchPostResult> PostBatchAsync(JournalBatchPostRequest request)
    {
        var result = new JournalBatchPostResult
        {
            TotalCount = request.Journals.Count,
            Results = new List<JournalPostResult>()
        };

        foreach (var journal in request.Journals)
        {
            var postResult = await PostAsync(journal);
            result.Results.Add(postResult);
            if (postResult.Success)
                result.SuccessCount++;
            else
            {
                result.FailureCount++;
                result.Errors.Add(new JournalBatchError
                {
                    Index = result.Results.Count - 1,
                    DocumentNumber = postResult.DocumentNumber,
                    ErrorMessage = postResult.Message,
                    ErrorCode = "POST_FAILED"
                });
            }
        }

        result.AllSucceeded = result.FailureCount == 0;
        return result;
    }

    public async Task<JournalReverseResult> ReverseAsync(JournalReverseRequest request)
    {
        var originalLines = await _db.UniversalJournals
            .Where(j => j.DocumentNumber == request.DocumentNumber)
            .ToListAsync();

        if (!originalLines.Any())
        {
            return new JournalReverseResult
            {
                Success = false,
                Message = $"Document {request.DocumentNumber} not found"
            };
        }

        var reversalDocNumber = GenerateDocumentNumber();
        var now = DateTime.UtcNow;
        int lineNum = 1;

        foreach (var line in originalLines)
        {
            var reversal = new UniversalJournalEntity
            {
                Id = Guid.NewGuid(),
                FiscalYear = int.TryParse(request.FiscalYear, out var fy) ? fy : now.Year,
                Period = now.Month,
                DocumentNumber = reversalDocNumber,
                DocumentType = "REVERSAL",
                DocumentDate = now,
                PostingDate = request.PostingDate,
                LineNumber = lineNum++,
                AccountCode = line.AccountCode,
                DebitAmount = line.CreditAmount,
                CreditAmount = line.DebitAmount,
                Currency = line.Currency,
                ExchangeRate = line.ExchangeRate,
                AmountLC = -line.AmountLC,
                CostCenter = line.CostCenter,
                ProfitCenter = line.ProfitCenter,
                Plant = line.Plant,
                MaterialCode = line.MaterialCode,
                Reference = request.Reason,
                Description = $"Reversal of {request.DocumentNumber}",
                ReversalDocument = request.DocumentNumber,
                IsReversal = true,
                PostedAt = now,
                CreatedBy = "SYSTEM",
                Status = "Posted",
                Hash = ComputeJournalHash(reversalDocNumber, lineNum, line.AccountCode, line.CreditAmount, line.DebitAmount)
            };

            _db.UniversalJournals.Add(reversal);
        }

        if (request.ReverseOriginal)
        {
            foreach (var line in originalLines)
            {
                line.Status = "Reversed";
            }
        }

        await _db.SaveChangesAsync();

        return new JournalReverseResult
        {
            Success = true,
            ReversalDocumentNumber = reversalDocNumber,
            Message = $"Reversal document {reversalDocNumber} created for {request.DocumentNumber}"
        };
    }

    public async Task<TrialBalanceResult> GetTrialBalanceAsync(TrialBalanceRequest request)
    {
        var query = _db.UniversalJournals
            .Where(j => j.DocumentNumber != ""
                && j.FiscalYear.ToString() == request.FiscalYear
                && j.Period <= request.FiscalPeriod);

        if (!string.IsNullOrEmpty(request.GlAccountFrom))
            query = query.Where(j => j.AccountCode.CompareTo(request.GlAccountFrom) >= 0);
        if (!string.IsNullOrEmpty(request.GlAccountTo))
            query = query.Where(j => j.AccountCode.CompareTo(request.GlAccountTo) <= 0);
        if (!string.IsNullOrEmpty(request.CostCenter))
            query = query.Where(j => j.CostCenter == request.CostCenter);

        var grouped = await query
            .GroupBy(j => new { j.AccountCode, j.Currency })
            .Select(g => new
            {
                AccountCode = g.Key.AccountCode,
                Currency = g.Key.Currency,
                Debit = g.Sum(j => j.DebitAmount),
                Credit = g.Sum(j => j.CreditAmount)
            })
            .ToListAsync();

        var lineItems = grouped
            .Where(g => request.IncludeZeroBalances || g.Debit != 0 || g.Credit != 0)
            .Select(g => new TrialBalanceLineItem
            {
                GlAccount = g.AccountCode,
                AccountName = g.AccountCode,
                AccountType = DetermineAccountType(g.AccountCode),
                DebitAmount = g.Debit,
                CreditAmount = g.Credit,
                ClosingBalance = g.Debit - g.Credit,
                Currency = g.Currency
            })
            .ToList();

        var totalDebit = lineItems.Sum(l => l.DebitAmount);
        var totalCredit = lineItems.Sum(l => l.CreditAmount);

        return new TrialBalanceResult
        {
            CompanyCode = request.CompanyCode,
            FiscalYear = request.FiscalYear,
            FiscalPeriod = request.FiscalPeriod,
            LineItems = lineItems,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            TotalBalance = totalDebit - totalCredit,
            IsBalanced = Math.Abs(totalDebit - totalCredit) < 0.01m
        };
    }

    public async Task<ProfitAndLossResult> GetProfitAndLossAsync(ProfitAndLossRequest request)
    {
        var query = _db.UniversalJournals
            .Where(j => j.FiscalYear.ToString() == request.FiscalYear
                && j.Period >= request.PeriodFrom && j.Period <= request.PeriodTo);

        if (!string.IsNullOrEmpty(request.ProfitCenter))
            query = query.Where(j => j.ProfitCenter == request.ProfitCenter);

        var entries = await query.ToListAsync();

        var revenue = entries.Where(j => j.AccountCode.StartsWith("4")).Sum(j => j.CreditAmount - j.DebitAmount);
        var expenses = entries.Where(j => j.AccountCode.StartsWith("5") || j.AccountCode.StartsWith("6")).Sum(j => j.DebitAmount - j.CreditAmount);

        return new ProfitAndLossResult
        {
            CompanyCode = request.CompanyCode,
            FiscalYear = request.FiscalYear,
            TotalRevenue = revenue,
            TotalExpenses = expenses,
            NetIncome = revenue - expenses,
            Categories = new List<PnLCategory>
            {
                new() { CategoryName = "Revenue", CategoryType = "Revenue", Amount = revenue },
                new() { CategoryName = "Expenses", CategoryType = "Expense", Amount = expenses }
            }
        };
    }

    public async Task<BalanceSheetResult> GetBalanceSheetAsync(BalanceSheetRequest request)
    {
        var query = _db.UniversalJournals
            .Where(j => j.FiscalYear.ToString() == request.FiscalYear && j.Period <= request.FiscalPeriod);

        var entries = await query.ToListAsync();
        var assets = entries.Where(j => j.AccountCode.StartsWith("1")).Sum(j => j.DebitAmount - j.CreditAmount);
        var liabilities = entries.Where(j => j.AccountCode.StartsWith("2")).Sum(j => j.CreditAmount - j.DebitAmount);
        var equity = entries.Where(j => j.AccountCode.StartsWith("3")).Sum(j => j.CreditAmount - j.DebitAmount);

        return new BalanceSheetResult
        {
            CompanyCode = request.CompanyCode,
            FiscalYear = request.FiscalYear,
            TotalAssets = assets,
            TotalLiabilities = liabilities,
            TotalEquity = equity,
            IsBalanced = Math.Abs(assets - liabilities - equity) < 0.01m
        };
    }

    public async Task<CostCenterReportResult> GetCostCenterReportAsync(CostCenterReportRequest request)
    {
        var query = _db.UniversalJournals
            .Where(j => j.FiscalYear.ToString() == request.FiscalYear
                && j.Period >= request.PeriodFrom
                && j.Period <= request.PeriodTo
                && !string.IsNullOrEmpty(j.CostCenter));

        if (!string.IsNullOrEmpty(request.CostCenterFrom))
            query = query.Where(j => j.CostCenter.CompareTo(request.CostCenterFrom) >= 0);
        if (!string.IsNullOrEmpty(request.CostCenterTo))
            query = query.Where(j => j.CostCenter.CompareTo(request.CostCenterTo) <= 0);

        var grouped = await query
            .GroupBy(j => j.CostCenter)
            .Select(g => new CostCenterReportLineItem
            {
                CostCenter = g.Key,
                CostCenterName = g.Key,
                Category = "Actual",
                ActualAmount = g.Sum(j => j.DebitAmount),
                PlannedAmount = g.Sum(j => j.CreditAmount),
                Variance = g.Sum(j => j.DebitAmount) - g.Sum(j => j.CreditAmount)
            })
            .ToListAsync();

        foreach (var item in grouped)
        {
            item.VariancePercentage = item.PlannedAmount != 0
                ? Math.Round(item.Variance / item.PlannedAmount * 100, 2)
                : 0;
        }

        return new CostCenterReportResult
        {
            CompanyCode = request.CompanyCode,
            LineItems = grouped,
            TotalPlanned = grouped.Sum(i => i.PlannedAmount),
            TotalActual = grouped.Sum(i => i.ActualAmount),
            TotalVariance = grouped.Sum(i => i.Variance)
        };
    }

    public async Task<ProfitCenterReportResult> GetProfitCenterReportAsync(ProfitCenterReportRequest request)
    {
        var query = _db.UniversalJournals
            .Where(j => j.FiscalYear.ToString() == request.FiscalYear
                && j.Period >= request.PeriodFrom
                && j.Period <= request.PeriodTo
                && !string.IsNullOrEmpty(j.ProfitCenter));

        var grouped = await query
            .GroupBy(j => j.ProfitCenter)
            .Select(g => new ProfitCenterReportLineItem
            {
                ProfitCenter = g.Key,
                ProfitCenterName = g.Key,
                Revenue = g.Sum(j => j.CreditAmount),
                CostOfGoodsSold = g.Sum(j => j.DebitAmount)
            })
            .ToListAsync();

        return new ProfitCenterReportResult
        {
            CompanyCode = request.CompanyCode,
            LineItems = grouped,
            TotalRevenue = grouped.Sum(i => i.Revenue),
            TotalCost = grouped.Sum(i => i.CostOfGoodsSold),
            TotalProfit = grouped.Sum(i => i.Revenue - i.CostOfGoodsSold)
        };
    }

    public Task<JournalSimulateResult> SimulateAsync(JournalSimulateRequest request)
    {
        var totalDebit = request.LineItems.Sum(l => l.DebitAmount);
        var totalCredit = request.LineItems.Sum(l => l.CreditAmount);

        var result = new JournalSimulateResult
        {
            IsValid = Math.Abs(totalDebit - totalCredit) < 0.01m,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            BalanceDifference = totalDebit - totalCredit,
            SimulatedLines = request.LineItems.Select(l => new SimulatedLineItem
            {
                GlAccount = l.GlAccount,
                AccountName = l.GlAccount,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Currency = l.Currency
            }).ToList()
        };

        if (!result.IsValid)
            result.Errors.Add($"Debit/Credit mismatch: {result.BalanceDifference}");

        return Task.FromResult(result);
    }

    public Task<JournalValidateResult> ValidateAsync(JournalValidateRequest request)
    {
        var messages = new List<ValidationMessage>();
        var totalDebit = request.LineItems.Sum(l => l.DebitAmount);
        var totalCredit = request.LineItems.Sum(l => l.CreditAmount);

        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            messages.Add(new ValidationMessage { Type = "Error", Code = "BALANCE_MISMATCH", Message = "Debit and Credit totals do not match" });

        if (!request.LineItems.Any())
            messages.Add(new ValidationMessage { Type = "Error", Code = "NO_LINES", Message = "Journal must have at least one line item" });

        return Task.FromResult(new JournalValidateResult
        {
            IsValid = !messages.Any(m => m.Type == "Error"),
            Messages = messages
        });
    }

    private static string GenerateDocumentNumber()
    {
        return $"DOC{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private static string DetermineAccountType(string accountCode)
    {
        if (accountCode.StartsWith("1")) return "Asset";
        if (accountCode.StartsWith("2")) return "Liability";
        if (accountCode.StartsWith("3")) return "Equity";
        if (accountCode.StartsWith("4")) return "Revenue";
        if (accountCode.StartsWith("5") || accountCode.StartsWith("6")) return "Expense";
        return "Unknown";
    }

    private static string ComputeJournalHash(string docNumber, int line, string account, decimal debit, decimal credit)
    {
        var data = $"{docNumber}|{line}|{account}|{debit}|{credit}|{DateTime.UtcNow:O}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
