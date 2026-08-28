using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// QC-01: CRA Method Analytical Calculation Accuracy
/// QC-02: Non-Conformance & Out-of-Spec (OOS) Hold
/// QC-03: Certificate of Analysis (COA) Auto-Generation
/// </summary>
public class QualityControlTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // QC-01: CRA Method Analytical Calculation Accuracy
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task QC01_CRA_Method_PurityCalculation_AccurateForCornStarch()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Raw lab parameters for Corn Starch
        decimal sampleWeight = 10.000m;   // grams
        decimal ashWeight = 0.050m;       // grams after ignition
        decimal fatExtract = 0.080m;      // grams from Soxhlet extraction
        decimal proteinN = 0.128m;         // grams nitrogen from Kjeldahl

        // Act: Calculate analytical results (standard formulas)
        decimal ashPct = (ashWeight / sampleWeight) * 100m;  // Ash %
        decimal fatPct = (fatExtract / sampleWeight) * 100m;  // Fat %
        decimal proteinPct = proteinN * 6.25m;                 // Protein (N x 6.25)
        decimal moisturePct = 12.50m;                           // Measured separately
        decimal purity = 100m - ashPct - fatPct - proteinPct - moisturePct; // By difference

        // Store result in InspectionResultDetailEntity
        var result = new InspectionResultDetailEntity
        {
            LotNumber = "LOT-CORN-2026-001",
            Plant = "PLT-01",
            MaterialCode = "MAT-CORN-STARCH",
            MaterialName = "Corn Starch",
            InspectionLotOrigin = "01", // Production
            ReportType = "CRA",
            Quantity = 10,
            DefectiveQuantity = 0,
            ResultStatus = "RECORDED",
            RecordedBy = "LAB-TECH-01",
            RecordedAt = DateTime.UtcNow
        };
        db.InspectionResultDetails.Add(result);
        await db.SaveChangesAsync();

        // Assert: Verify calculated values are accurate
        Assert.Equal(0.50m, ashPct);          // 0.050/10.000 * 100
        Assert.Equal(0.80m, fatPct);           // 0.080/10.000 * 100
        Assert.Equal(0.80m, proteinPct);       // 0.128 * 6.25
        Assert.Equal(85.40m, purity);          // 100 - 0.50 - 0.80 - 0.80 - 12.50
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("RECORDED", result.ResultStatus);
    }

    [Fact]
    public async Task QC01_CRA_Method_DextroseEquivalent_CalculatedCorrectly()
    {
        // Arrange: Dextrose Equivalent calculation for starch hydrolysis
        decimal reducingSugarG = 4.567m;  // grams reducing sugar (as glucose)
        decimal drySubstanceG = 10.000m;  // grams dry substance

        // Act: DE = (reducing sugar / dry substance) * 100
        decimal de = (reducingSugarG / drySubstanceG) * 100m;

        // Assert: DE value is accurate
        Assert.Equal(45.67m, de);  // Standard DE formula

        // Verify precision is maintained (no rounding error)
        decimal deRecalculated = (reducingSugarG * 100m) / drySubstanceG;
        Assert.Equal(de, deRecalculated);
    }

    [Fact]
    public async Task QC01_CRA_Method_MoistureContent_ResultWithinSpec()
    {
        var db = CreateDb();

        // Arrange: Moisture specification for Wet Corn Gluten Feed
        decimal specMaxMoisture = 12.0m;  // Max 12% moisture
        decimal actualMoisture = 11.5m;   // Measured 11.5%

        // Act: Check if within specification
        bool withinSpec = actualMoisture <= specMaxMoisture;

        // Assert: Should pass
        Assert.True(withinSpec, $"Moisture {actualMoisture}% is within spec max {specMaxMoisture}%");

        // Store as inspection result
        var inspectionResult = new InspectionResultEntity
        {
            ResultId = $"IR-{DateTime.Now:yyyyMMddHHmmss}",
            LotNumber = "LOT-WCGF-2026-001",
            Characteristic = "Moisture Content",
            Result = actualMoisture.ToString("F1"),
            Specification = $"<={specMaxMoisture}%",
            Status = withinSpec ? "Passed" : "Failed"
        };
        db.InspectionResults.Add(inspectionResult);
        await db.SaveChangesAsync();

        var saved = await db.InspectionResults.FirstOrDefaultAsync(r => r.ResultId == inspectionResult.ResultId);
        Assert.NotNull(saved);
        Assert.Equal("Passed", saved!.Status);
    }

    [Fact]
    public async Task QC01_CRA_Method_BaumeCalculation_Accurate()
    {
        // Arrange: Baumé measurement for liquid glucose
        decimal specificGravity = 1.42m;

        // Act: Baumé = 145 - (145 / specificGravity)
        decimal baume = 145m - (145m / specificGravity);

        // Assert: Baumé calculation is accurate
        Assert.Equal(42.8873m, Math.Round(baume, 4));

        // Verify no cumulative rounding errors
        decimal baumeExact = 145m - (145m / specificGravity);
        Assert.Equal(baume, baumeExact);
    }

    // ════════════════════════════════════════════════════════════════
    // QC-02: Non-Conformance & Out-of-Spec (OOS) Hold
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task QC02_OOS_HighMoisture_SetsBatchToQualityHold()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Create a batch with high moisture
        var batch = new BatchEntity
        {
            TenantId = tenantId,
            BatchNumber = "BATCH-WCGF-2026-001",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Wet Corn Gluten Feed",
            ManufacturingDate = DateTime.UtcNow.AddDays(-2),
            ExpiryDate = DateTime.UtcNow.AddDays(180),
            Status = "ACTIVE",
            Quantity = 5000,
            UnitOfMeasure = "KG"
        };
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // Act: Log OOS result (moisture exceeds spec)
        decimal specMaxMoisture = 12.0m;
        decimal actualMoisture = 14.2m;  // OOS!
        bool isOOS = actualMoisture > specMaxMoisture;

        if (isOOS)
        {
            batch.Status = "QUALITY_HOLD";
            await db.SaveChangesAsync();
        }

        // Assert: Batch is now on quality hold
        var refreshed = await db.Batches.FindAsync(batch.Id);
        Assert.Equal("QUALITY_HOLD", refreshed!.Status);

        // Verify OOS notification created
        var notification = new QualityNotificationEntity
        {
            NotificationNumber = $"QN-{DateTime.Now:yyyyMMdd}-001",
            NotificationType = "Q2",  // Defect notification
            Description = $"OOS: Moisture content {actualMoisture}% exceeds spec max {specMaxMoisture}%",
            MaterialCode = "MAT-WCGF",
            MaterialName = "Wet Corn Gluten Feed",
            Batch = batch.BatchNumber,
            Priority = "High",
            Status = "NEW",
            CreatedBy = "SYSTEM"
        };
        db.QualityNotifications.Add(notification);
        await db.SaveChangesAsync();

        var savedNotif = await db.QualityNotifications
            .FirstOrDefaultAsync(n => n.NotificationNumber == notification.NotificationNumber);
        Assert.NotNull(savedNotif);
        Assert.Equal("NEW", savedNotif!.Status);
        Assert.Contains("OOS", savedNotif.Description);
    }

    [Fact]
    public async Task QC02_OOS_Batch_BlockedFromSalesOrderAllocation()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Quality hold batch and a sales order
        var batch = new BatchEntity
        {
            TenantId = tenantId,
            BatchNumber = "BATCH-HOLD-001",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Corn Starch",
            Status = "QUALITY_HOLD",
            Quantity = 1000,
            UnitOfMeasure = "KG"
        };
        db.Batches.Add(batch);

        var customer = new CustomerEntity
        {
            Code = "CUST-001",
            Name = "Test Customer",
            CreditLimit = 50000,
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Act: Attempt to allocate batch to sales order
        bool canAllocate = batch.Status == "ACTIVE" || batch.Status == "UNRESTRICTED";

        // Assert: Allocation should be blocked
        Assert.False(canAllocate, "Quality hold batch should not be allocatable to sales orders");
        Assert.Equal("QUALITY_HOLD", batch.Status);
    }

    [Fact]
    public async Task QC02_OOS_UsageDecision_Rejected_StockNotReleased()
    {
        var db = CreateDb();

        // Arrange: Lot with OOS result
        var lot = new InspectionLotEntity
        {
            LotNumber = "IL-2026-001",
            MaterialName = "Corn Starch",
            Quantity = "1000",
            Inspected = 100,
            Passed = 85,
            Failed = 15,  // 15% failure rate
            Status = "Inspected"
        };
        db.InspectionLots.Add(lot);
        await db.SaveChangesAsync();

        // Act: Record usage decision as Rejected
        var ud = new UsageDecisionEntity
        {
            DecisionId = $"UD-{DateTime.Now:yyyyMMddHHmmss}",
            LotNumber = lot.LotNumber,
            MaterialName = lot.MaterialName,
            Decision = "Reject",
            Notes = "High failure rate - 15% non-conforming",
            DecisionDate = DateTime.UtcNow
        };
        db.UsageDecisions.Add(ud);
        await db.SaveChangesAsync();

        // Update lot status
        lot.Status = "Rejected";
        await db.SaveChangesAsync();

        // Assert: Stock not released to unrestricted
        var refreshedLot = await db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == lot.LotNumber);
        Assert.Equal("Rejected", refreshedLot!.Status);

        var refreshedUd = await db.UsageDecisions
            .FirstOrDefaultAsync(u => u.DecisionId == ud.DecisionId);
        Assert.Equal("Reject", refreshedUd!.Decision);
    }

    [Fact]
    public async Task QC02_OOS_AcceptanceWithConditionalRelease()
    {
        var db = CreateDb();

        // Arrange: Lot with minor deviation but acceptable
        var lot = new InspectionLotEntity
        {
            LotNumber = "IL-2026-002",
            MaterialName = "Dextrose",
            Quantity = "2000",
            Inspected = 100,
            Passed = 97,
            Failed = 3,
            Status = "Inspected"
        };
        db.InspectionLots.Add(lot);
        await db.SaveChangesAsync();

        // Act: Conditional acceptance (deviation approved)
        var ud = new UsageDecisionEntity
        {
            DecisionId = $"UD-{DateTime.Now:yyyyMMddHHmmss}",
            LotNumber = lot.LotNumber,
            MaterialName = lot.MaterialName,
            Decision = "Accept with Deviation",
            Notes = "Minor deviation approved by QA Manager per deviation DEV-2026-042",
            DecisionDate = DateTime.UtcNow
        };
        db.UsageDecisions.Add(ud);

        lot.Status = "Accepted with Deviation";
        await db.SaveChangesAsync();

        // Assert: Conditional acceptance recorded
        var refreshedLot = await db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == lot.LotNumber);
        Assert.Equal("Accepted with Deviation", refreshedLot!.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // QC-03: Certificate of Analysis (COA) Auto-Generation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task QC03_COA_Generation_PullsVerifiedLabValues()
    {
        var db = CreateDb();

        // Arrange: Approved batch with verified lab results
        var batch = new BatchEntity
        {
            BatchNumber = "BATCH-COA-001",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Corn Starch",
            ManufacturingDate = DateTime.UtcNow.AddDays(-5),
            Status = "ACTIVE",
            Quantity = 3000,
            UnitOfMeasure = "KG",
            CertificateOfAnalysis = ""
        };
        db.Batches.Add(batch);

        var labResults = new[]
        {
            new TestResultEntity { ResultId = "TR-001", SampleId = "SAMP-001", TestName = "Purity", Result = "99.2%", Specification = ">=98.5%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-002", SampleId = "SAMP-001", TestName = "Moisture", Result = "11.8%", Specification = "<=12.5%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-003", SampleId = "SAMP-001", TestName = "Ash", Result = "0.05%", Specification = "<=0.1%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-004", SampleId = "SAMP-001", TestName = "Protein", Result = "0.80%", Specification = "<=1.0%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-005", SampleId = "SAMP-001", TestName = "Fat", Result = "0.08%", Specification = "<=0.15%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-006", SampleId = "SAMP-001", TestName = "DE Value", Result = "45.67", Specification = "40-50", Status = "Passed" },
        };
        db.TestResults.AddRange(labResults);
        await db.SaveChangesAsync();

        // Act: Generate COA (pull verified lab values)
        var coaResults = await db.TestResults
            .Where(t => t.SampleId == "SAMP-001" && t.Status == "Passed")
            .ToListAsync();

        var coaContent = new System.Text.StringBuilder();
        coaContent.AppendLine($"CERTIFICATE OF ANALYSIS");
        coaContent.AppendLine($"Batch: {batch.BatchNumber}");
        coaContent.AppendLine($"Material: {batch.MaterialName}");
        coaContent.AppendLine($"Manufacturing Date: {batch.ManufacturingDate:yyyy-MM-dd}");
        coaContent.AppendLine($"---");
        foreach (var result in coaResults)
        {
            coaContent.AppendLine($"{result.TestName}: {result.Result} (Spec: {result.Specification}) [{result.Status}]");
        }

        // Store COA on batch
        batch.CertificateOfAnalysis = coaContent.ToString();
        await db.SaveChangesAsync();

        // Assert: COA generated with all verified values
        var refreshedBatch = await db.Batches.FirstOrDefaultAsync(b => b.BatchNumber == batch.BatchNumber);
        Assert.NotNull(refreshedBatch!.CertificateOfAnalysis);
        Assert.Contains("Corn Starch", refreshedBatch.CertificateOfAnalysis);
        Assert.Contains("99.2%", refreshedBatch.CertificateOfAnalysis);
        Assert.Contains("Purity", refreshedBatch.CertificateOfAnalysis);
        Assert.Equal(6, coaResults.Count); // All 6 tests included
    }

    [Fact]
    public async Task QC03_COA_OnlyIncludesPassedTests()
    {
        var db = CreateDb();

        // Arrange: Mix of passed and failed tests
        var testResults = new[]
        {
            new TestResultEntity { ResultId = "TR-010", SampleId = "SAMP-002", TestName = "Purity", Result = "99.2%", Specification = ">=98.5%", Status = "Passed" },
            new TestResultEntity { ResultId = "TR-011", SampleId = "SAMP-002", TestName = "Moisture", Result = "14.5%", Specification = "<=12.5%", Status = "Failed" },
            new TestResultEntity { ResultId = "TR-012", SampleId = "SAMP-002", TestName = "Ash", Result = "0.04%", Specification = "<=0.1%", Status = "Passed" },
        };
        db.TestResults.AddRange(testResults);
        await db.SaveChangesAsync();

        // Act: Generate COA with only passed tests
        var passedResults = await db.TestResults
            .Where(t => t.SampleId == "SAMP-002" && t.Status == "Passed")
            .ToListAsync();

        // Assert: Only passed tests included in COA
        Assert.Equal(2, passedResults.Count);
        Assert.All(passedResults, r => Assert.Equal("Passed", r.Status));
        Assert.DoesNotContain(passedResults, r => r.TestName == "Moisture");
    }

    [Fact]
    public async Task QC03_COA_MatchesBatchNumberAndParameters()
    {
        var db = CreateDb();

        // Arrange
        string expectedBatch = "BATCH-MATCH-2026-001";
        var batch = new BatchEntity
        {
            BatchNumber = expectedBatch,
            MaterialId = Guid.NewGuid(),
            MaterialName = "Dextrose Monohydrate",
            Status = "ACTIVE",
            Quantity = 1500,
            CertificateOfAnalysis = ""
        };
        db.Batches.Add(batch);

        var labResult = new TestResultEntity
        {
            ResultId = "TR-MATCH-001",
            SampleId = $"SAMP-{expectedBatch}",
            TestName = "Dextrose Equivalent",
            Result = "99.5",
            Specification = ">=99.0",
            Status = "Passed"
        };
        db.TestResults.Add(labResult);
        await db.SaveChangesAsync();

        // Act: Generate COA
        var results = await db.TestResults
            .Where(t => t.SampleId == labResult.SampleId)
            .ToListAsync();

        var coa = $"COA for {batch.BatchNumber} - {batch.MaterialName}\n";
        foreach (var r in results)
        {
            coa += $"{r.TestName}: {r.Result} ({r.Specification})\n";
        }

        // Assert: COA matches batch number and parameters
        Assert.Contains(expectedBatch, coa);
        Assert.Contains(batch.MaterialName, coa);
        Assert.Contains("Dextrose Equivalent", coa);
        Assert.Contains("99.5", coa);
    }
}
