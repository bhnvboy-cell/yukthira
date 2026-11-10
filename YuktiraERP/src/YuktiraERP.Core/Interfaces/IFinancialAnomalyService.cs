namespace YuktiraERP.Core.Interfaces;

public class AnomalyFlagDto
{
    public string EntityType { get; set; } = "JournalEntry";
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string AccountOrVendor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double ZScore { get; set; }
    public double RiskScore { get; set; } // 0.0 to 100.0
    public string AnomalyType { get; set; } = string.Empty; // "OutlierAmount", "RoundSumOverride", "WeekendPosting", "BenfordDeviation"
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // "Low", "Medium", "High", "Critical"
}

public class BenfordDigitAnalysis
{
    public int Digit { get; set; }
    public double ExpectedPercent { get; set; }
    public double ActualPercent { get; set; }
    public double Deviation { get; set; }
}

public class FinancialAnomalyReportDto
{
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public int TotalScanned { get; set; }
    public int TotalAnomalies { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public double OverallRiskIndex { get; set; }
    public List<AnomalyFlagDto> Anomalies { get; set; } = new();
    public List<BenfordDigitAnalysis> BenfordDistribution { get; set; } = new();
}

public interface IFinancialAnomalyService
{
    Task<FinancialAnomalyReportDto> ScanJournalAnomaliesAsync(Guid tenantId, DateTime fromDate, DateTime toDate, double thresholdZ = 2.5);
    Task<FinancialAnomalyReportDto> ScanProcurementAnomaliesAsync(Guid tenantId, double thresholdZ = 2.5);
}
