using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

/// <summary>
/// SPC-01: Subgroup statistics (mean, range, sample standard deviation)
/// SPC-02: Control limits from standard SPC constants (A2, D3, D4, B3, B4, d2)
/// SPC-03: Process capability formulas (Cp, Cpk, Pp, Ppk)
/// SPC-04: Nelson / Western Electric run-rule detection
/// SPC-05: Empty-data behaviour of the analysis service
/// </summary>
public class SpcEngineTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private Mock<ITenantContext> CreateTenantContext(Guid tenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(t => t.TenantId).Returns(tenantId);
        return mock;
    }

    private static SpcSubgroupDto Subgroup(int index, double mean, double range)
        => new SpcSubgroupDto
        {
            Index = index,
            LotNumber = "LOT-" + index,
            Label = "LOT-" + index,
            N = 4,
            Mean = mean,
            Range = range,
            StdDev = range / 2.0
        };

    // ════════════════════════════════════════════════════════════════
    // SPC-01: Subgroup statistics
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SPC01_SubgroupStats_MeanRangeAndSampleStdDev_Correct()
    {
        // Arrange: five thickness readings around 2.00 mm
        var values = new List<decimal> { 2.00m, 2.02m, 1.98m, 2.01m, 1.99m };

        // Act
        var subgroup = SpcEngineService.BuildSubgroup(1, "LOT-THK-001", new DateTime(2026, 9, 1), values);

        // Assert
        Assert.Equal(5, subgroup.N);
        Assert.Equal(2.00, subgroup.Mean, 6);
        Assert.NotNull(subgroup.Range);
        Assert.Equal(0.04, subgroup.Range!.Value, 6);
        Assert.NotNull(subgroup.StdDev);
        // sample stdev = sqrt(sum((x - mean)^2) / (n - 1)) = sqrt(0.001 / 4)
        Assert.Equal(0.015811, subgroup.StdDev!.Value, 6);
        Assert.Equal("LOT-THK-001", subgroup.Label);
    }

    [Fact]
    public void SPC01_SubgroupStats_SingleMeasurement_ReturnsNullRangeAndStdDev()
    {
        // Act: n = 1 subgroups have no range / standard deviation
        var subgroup = SpcEngineService.BuildSubgroup(1, "LOT-THK-002", new DateTime(2026, 9, 2), new List<decimal> { 2.00m });

        // Assert
        Assert.Equal(1, subgroup.N);
        Assert.Equal(2.00, subgroup.Mean, 6);
        Assert.Null(subgroup.Range);
        Assert.Null(subgroup.StdDev);
    }

    // ════════════════════════════════════════════════════════════════
    // SPC-02: Control limits
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SPC02_ControlLimits_XbarRangeAndS_UseSpcConstants()
    {
        // Arrange: grand mean 2.00, R-bar 0.04, subgroup size n = 5
        var grandMean = 2.00;
        var rBar = 0.04;
        var sBar = 0.02;

        // Act
        var xbar = SpcEngineService.XbarLimits(grandMean, rBar, 5);
        var rChart = SpcEngineService.RChartLimits(rBar, 5);
        var rChartN7 = SpcEngineService.RChartLimits(rBar, 7);
        var sChart = SpcEngineService.SChartLimits(sBar, 5);

        // Assert: A2(5) = 0.577, D4(5) = 2.114, D3(5) = 0, B4(5) = 2.089, B3(5) = 0
        Assert.Equal(2.02308, xbar.Ucl, 6);
        Assert.Equal(2.00, xbar.Cl, 6);
        Assert.Equal(1.97692, xbar.Lcl, 6);

        Assert.Equal(0.08456, rChart.Ucl, 6);
        Assert.Equal(0.04, rChart.Cl, 6);
        Assert.Equal(0.0, rChart.Lcl, 6);

        // D3(7) = 0.076 -> LCL = 0.00304
        Assert.Equal(0.00304, rChartN7.Lcl, 6);

        Assert.Equal(0.04178, sChart.Ucl, 6);
        Assert.Equal(0.02, sChart.Cl, 6);
        Assert.Equal(0.0, sChart.Lcl, 6);

        // d2(5) = 2.326 -> sigma within = R-bar / d2 = 0.04 / 2.326
        var sigmaWithin = rBar / SpcEngineService.GetD2(5);
        Assert.Equal(0.017197, sigmaWithin, 6);
    }

    // ════════════════════════════════════════════════════════════════
    // SPC-03: Capability formulas
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SPC03_Capability_KnownInputs_MatchFormulas()
    {
        // Arrange: LSL 1.90, USL 2.10, mu 2.00, sigma_within 0.02, sigma_overall 0.03
        // Act
        var cap = SpcEngineService.ComputeCapability(1.90, 2.10, 2.00, 0.02, 0.03);

        // Assert: Cp = (USL-LSL)/(6*sw) = 0.20/0.12, Pp = 0.20/0.18
        Assert.NotNull(cap);
        Assert.Equal(1.6667, cap!.Cp!.Value, 4);
        Assert.Equal(1.6667, cap.Cpk!.Value, 4);
        Assert.Equal(1.1111, cap.Pp!.Value, 4);
        Assert.Equal(1.1111, cap.Ppk!.Value, 4);
        Assert.Equal(1.90, cap.Lsl!.Value, 6);
        Assert.Equal(2.10, cap.Usl!.Value, 6);

        // Off-centre process: Cpk = min((USL-mu)/(3*sw), (mu-LSL)/(3*sw)) = min(0.8333, 2.5)
        var shifted = SpcEngineService.ComputeCapability(1.90, 2.10, 2.05, 0.02, 0.03);
        Assert.NotNull(shifted);
        Assert.Equal(0.8333, shifted!.Cpk!.Value, 4);
        Assert.True(shifted.Cpk.Value < shifted.Cp!.Value, "Cpk must not exceed Cp for an off-centre process");
    }

    [Fact]
    public void SPC03_Capability_DegenerateOrZeroSigma_ReturnsNullIndices()
    {
        // Degenerate specification limits -> no capability at all
        Assert.Null(SpcEngineService.ComputeCapability(2.00, 2.00, 2.00, 0.02, 0.03));
        Assert.Null(SpcEngineService.ComputeCapability(0, 0, 0, 0.02, 0.03));

        // Sigma = 0 -> every index stays null (no divide-by-zero)
        var zeroSigma = SpcEngineService.ComputeCapability(1.90, 2.10, 2.00, 0, 0);
        Assert.NotNull(zeroSigma);
        Assert.Null(zeroSigma!.Cp);
        Assert.Null(zeroSigma.Cpk);
        Assert.Null(zeroSigma.Pp);
        Assert.Null(zeroSigma.Ppk);
    }

    // ════════════════════════════════════════════════════════════════
    // SPC-04: Run rules
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SPC04_NelsonRule2_NineConsecutiveSameSide_Detected()
    {
        // Arrange: 4 points below CL, 9 points above CL, 1 point below CL
        var raw = new double[] { -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, -1 };
        var points = raw.Select((value, index) => new SpcPointDto
        {
            Index = index,
            Label = "LOT-" + (index + 1),
            Value = value,
            Cl = 0,
            Ucl = 3,
            Lcl = -3
        }).ToList();

        // Act
        var violations = SpcEngineService.EvaluateRules(points);

        // Assert: Nelson rule 2 fires on points 5..13 (zero-based 4..12)
        Assert.NotEmpty(violations);
        var rule2 = violations.FirstOrDefault(v => v.RuleId == "2");
        Assert.NotNull(rule2);
        Assert.True(rule2!.PointIndexes.SequenceEqual(Enumerable.Range(4, 9)),
            "Rule 2 indexes were: " + string.Join(",", rule2.PointIndexes));
        Assert.Equal("Nine Points Same Side", rule2.RuleName);
        Assert.False(violations.Any(v => v.RuleId == "1"), "No point is beyond the control limits");
    }

    [Fact]
    public void SPC04_NelsonRule1_BeyondLimit_DetectedAndPointMarkedViolating()
    {
        // Arrange: 10 healthy subgroups plus one mean above the upper control limit
        var means = new List<double> { 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.10 };
        var subgroups = means.Select((mean, i) => Subgroup(i + 1, mean, 0.04)).ToList();

        // Act
        var chart = SpcEngineService.BuildXbarChart(subgroups, "X̄ Chart — Thickness", "mm");
        var violations = SpcEngineService.EvaluateRules(chart.Points);

        // Assert: only the outlying subgroup is flagged on the chart
        Assert.Equal(11, chart.Points.Count);
        Assert.Single(chart.Points.Where(p => p.Violating));
        Assert.True(chart.Points[10].Violating);

        // Nelson rule 1 reports that single point
        var rule1 = violations.FirstOrDefault(v => v.RuleId == "1");
        Assert.NotNull(rule1);
        Assert.True(rule1!.PointIndexes.SequenceEqual(new List<int> { 10 }),
            "Rule 1 indexes were: " + string.Join(",", rule1.PointIndexes));

        // Control limits are sane: UCL > CL > LCL and the healthy means sit inside
        var limits = chart.Points[0];
        Assert.True(limits.Ucl!.Value > limits.Cl!.Value);
        Assert.True(limits.Cl!.Value > limits.Lcl!.Value);
        Assert.True(limits.Lcl!.Value < 2.00 && 2.00 < limits.Ucl!.Value);
    }

    // ════════════════════════════════════════════════════════════════
    // SPC-05: Empty data
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SPC05_AnalyzeAsync_EmptyData_ReturnsEmptyDto()
    {
        // Arrange: empty database, tenant-scoped service
        using var db = CreateDb();
        var service = new SpcEngineService(db, CreateTenantContext(Guid.NewGuid()).Object);

        // Act
        var analysis = await service.AnalyzeAsync(new SpcQueryDto());
        var filters = await service.GetFilterOptionsAsync();

        // Assert: empty payload, null capability, no violations
        Assert.NotNull(analysis);
        Assert.Equal(0, analysis.SubgroupCount);
        Assert.Equal(0, analysis.ResultCount);
        Assert.Empty(analysis.Subgroups);
        Assert.Empty(analysis.Charts.Xbar.Points);
        Assert.Empty(analysis.Charts.R.Points);
        Assert.Empty(analysis.Charts.S.Points);
        Assert.Null(analysis.Capability);
        Assert.Empty(analysis.Violations);
        Assert.False(analysis.OutOfControl);

        Assert.NotNull(filters);
        Assert.Empty(filters.Characteristics);
        Assert.Empty(filters.MaterialCodes);
        Assert.Empty(filters.Plants);
    }
}
