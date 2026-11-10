using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class FinancialAnomalyService : IFinancialAnomalyService
{
    private readonly YuktiraDbContext _db;

    public FinancialAnomalyService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<FinancialAnomalyReportDto> ScanJournalAnomaliesAsync(Guid tenantId, DateTime fromDate, DateTime toDate, double thresholdZ = 2.5)
    {
        var entries = await _db.UniversalJournals
            .Where(j => j.TenantId == tenantId && j.PostingDate >= fromDate && j.PostingDate <= toDate && !j.IsReversal)
            .OrderByDescending(j => j.PostingDate)
            .Take(1000)
            .ToListAsync();

        var report = new FinancialAnomalyReportDto
        {
            TotalScanned = entries.Count
        };

        if (entries.Count == 0) return report;

        var amounts = entries.Select(e => e.DebitAmount > 0 ? (double)e.DebitAmount : (double)e.CreditAmount).ToList();
        var mean = amounts.Average();
        var variance = amounts.Select(a => Math.Pow(a - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        if (stdDev == 0) stdDev = 1.0;

        // Benford's Law Analysis
        var digitCounts = new int[10];
        int validDigits = 0;
        foreach (var amt in amounts)
        {
            if (amt <= 0) continue;
            var str = amt.ToString("F0").TrimStart('0');
            if (str.Length > 0 && char.IsDigit(str[0]))
            {
                int firstDigit = str[0] - '0';
                if (firstDigit >= 1 && firstDigit <= 9)
                {
                    digitCounts[firstDigit]++;
                    validDigits++;
                }
            }
        }

        for (int d = 1; d <= 9; d++)
        {
            double expected = Math.Log10(1.0 + 1.0 / d) * 100.0;
            double actual = validDigits > 0 ? ((double)digitCounts[d] / validDigits) * 100.0 : 0.0;
            report.BenfordDistribution.Add(new BenfordDigitAnalysis
            {
                Digit = d,
                ExpectedPercent = Math.Round(expected, 2),
                ActualPercent = Math.Round(actual, 2),
                Deviation = Math.Round(Math.Abs(actual - expected), 2)
            });
        }

        // Anomaly Checks
        foreach (var e in entries)
        {
            var amt = (double)(e.DebitAmount > 0 ? e.DebitAmount : e.CreditAmount);
            var z = Math.Abs(amt - mean) / stdDev;

            // 1. Statistical Outlier
            if (z >= thresholdZ && amt > 0)
            {
                var isCritical = z >= 3.5;
                report.Anomalies.Add(new AnomalyFlagDto
                {
                    EntityType = "UniversalJournal",
                    DocumentNumber = e.DocumentNumber,
                    TransactionDate = e.PostingDate,
                    AccountOrVendor = $"{e.AccountCode} ({e.AccountName})",
                    Amount = (decimal)amt,
                    ZScore = Math.Round(z, 2),
                    RiskScore = Math.Min(100.0, Math.Round(z * 25.0, 1)),
                    AnomalyType = "OutlierAmount",
                    Severity = isCritical ? "Critical" : "High",
                    Description = $"Transaction amount of {amt:C} deviates by {z:F1} standard deviations from the historical mean ({mean:C})."
                });
            }

            // 2. Round-Sum Overrides (Forensic check)
            if (amt >= 10000 && amt % 5000 == 0)
            {
                report.Anomalies.Add(new AnomalyFlagDto
                {
                    EntityType = "UniversalJournal",
                    DocumentNumber = e.DocumentNumber,
                    TransactionDate = e.PostingDate,
                    AccountOrVendor = $"{e.AccountCode} ({e.AccountName})",
                    Amount = (decimal)amt,
                    ZScore = Math.Round(z, 2),
                    RiskScore = 65.0,
                    AnomalyType = "RoundSumOverride",
                    Severity = "Medium",
                    Description = $"Exact round sum transaction of {amt:C} detected. Standard forensic trigger for manual journal overrides."
                });
            }

            // 3. Weekend/Off-Hours Postings
            if ((e.PostingDate.DayOfWeek == DayOfWeek.Saturday || e.PostingDate.DayOfWeek == DayOfWeek.Sunday) && amt >= 5000)
            {
                report.Anomalies.Add(new AnomalyFlagDto
                {
                    EntityType = "UniversalJournal",
                    DocumentNumber = e.DocumentNumber,
                    TransactionDate = e.PostingDate,
                    AccountOrVendor = $"{e.AccountCode} ({e.AccountName})",
                    Amount = (decimal)amt,
                    ZScore = Math.Round(z, 2),
                    RiskScore = 75.0,
                    AnomalyType = "WeekendPosting",
                    Severity = "High",
                    Description = $"High-value transaction posted on weekend ({e.PostingDate:dddd, yyyy-MM-dd}). Potential unauthorized batch posting."
                });
            }
        }

        report.TotalAnomalies = report.Anomalies.Count;
        report.CriticalCount = report.Anomalies.Count(a => a.Severity == "Critical");
        report.HighCount = report.Anomalies.Count(a => a.Severity == "High");
        report.OverallRiskIndex = report.TotalAnomalies > 0
            ? Math.Round(report.Anomalies.Average(a => a.RiskScore), 1)
            : 0.0;

        return report;
    }

    public async Task<FinancialAnomalyReportDto> ScanProcurementAnomaliesAsync(Guid tenantId, double thresholdZ = 2.5)
    {
        var pos = await _db.PurchaseOrders
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(500)
            .ToListAsync();

        var report = new FinancialAnomalyReportDto
        {
            TotalScanned = pos.Count
        };

        if (pos.Count == 0) return report;

        var amounts = pos.Select(p => (double)p.TotalAmount).ToList();
        var mean = amounts.Average();
        var variance = amounts.Select(a => Math.Pow(a - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        if (stdDev == 0) stdDev = 1.0;

        foreach (var po in pos)
        {
            var amt = (double)po.TotalAmount;
            var z = Math.Abs(amt - mean) / stdDev;

            if (z >= thresholdZ && amt > 0)
            {
                report.Anomalies.Add(new AnomalyFlagDto
                {
                    EntityType = "PurchaseOrder",
                    DocumentNumber = po.PoNumber,
                    TransactionDate = po.CreatedAt,
                    AccountOrVendor = po.VendorName,
                    Amount = po.TotalAmount,
                    ZScore = Math.Round(z, 2),
                    RiskScore = Math.Min(100.0, Math.Round(z * 22.0, 1)),
                    AnomalyType = "ProcurementOutlier",
                    Severity = z >= 3.5 ? "Critical" : "High",
                    Description = $"PO {po.PoNumber} amount of {amt:C} exceeds normal procurement thresholds by {z:F1} standard deviations."
                });
            }

            // Split-PO threshold avoidance check: PO amounts just below approval limit (e.g. 48,000 - 49,999)
            if (amt >= 48000 && amt < 50000)
            {
                report.Anomalies.Add(new AnomalyFlagDto
                {
                    EntityType = "PurchaseOrder",
                    DocumentNumber = po.PoNumber,
                    TransactionDate = po.CreatedAt,
                    AccountOrVendor = po.VendorName,
                    Amount = po.TotalAmount,
                    ZScore = Math.Round(z, 2),
                    RiskScore = 70.0,
                    AnomalyType = "ThresholdAvoidance",
                    Severity = "High",
                    Description = $"PO {po.PoNumber} amount of {amt:C} sits immediately below executive threshold ($50,000). Inspect for potential PO splitting."
                });
            }
        }

        report.TotalAnomalies = report.Anomalies.Count;
        report.CriticalCount = report.Anomalies.Count(a => a.Severity == "Critical");
        report.HighCount = report.Anomalies.Count(a => a.Severity == "High");
        report.OverallRiskIndex = report.TotalAnomalies > 0
            ? Math.Round(report.Anomalies.Average(a => a.RiskScore), 1)
            : 0.0;

        return report;
    }
}
