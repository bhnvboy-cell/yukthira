using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class VisionQmGateTests
{
    private const string GateCharacteristic = "VISION_GATE_PHYSICAL_GRADE";

    private static YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new YuktiraDbContext(options);
    }

    private static Mock<IQualityVisionInspectionEngine> CreateVisionMock(VisionSeverity severity, VisionDefectType defect = VisionDefectType.None)
    {
        var vision = new Mock<IQualityVisionInspectionEngine>();
        vision.Setup(e => e.InspectImageAsync(It.IsAny<VisionInspectionRequest>()))
            .ReturnsAsync(new VisionInspectionResult
            {
                Success = true,
                Severity = severity,
                DetectedDefect = defect,
                ConfidenceScore = 0.97f
            });
        return vision;
    }

    private static VisionQmGateService CreateService(YuktiraDbContext db, Mock<IQualityVisionInspectionEngine> vision)
    {
        return new VisionQmGateService(
            db,
            vision.Object,
            new InventoryMovementService(db),
            new ZqmAutoLotGeneratorService(db),
            new ZqmNonConformanceService(db),
            new AuditService(db),
            Options.Create(new VisionQmOptions()));
    }

    private static byte[] FindPassingFrame()
    {
        for (var seed = 0; seed < 100000; seed++)
        {
            var bytes = new byte[64];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((i * 31 + seed * 7) % 256);
            }

            var (_, impurity) = VisionQmGateService.ComputePhysicalGrading(bytes);
            if (impurity <= 3.0m)
            {
                return bytes;
            }
        }

        throw new InvalidOperationException("Unable to find a deterministic passing frame");
    }

    private static VisionQmFrameRequest Frame(Guid tenantId, byte[] bytes, string materialCode)
    {
        return new VisionQmFrameRequest
        {
            TenantId = tenantId,
            UserId = Guid.NewGuid().ToString(),
            ImageBase64 = Convert.ToBase64String(bytes),
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} Name",
            Plant = "1000",
            Quantity = 5,
            Unit = "EA"
        };
    }

    [Fact]
    public void VG01_SameFrameBytes_ProduceIdenticalGradeAndImpurity()
    {
        var frame = FindPassingFrame();

        var first = VisionQmGateService.ComputePhysicalGrading(frame);
        var second = VisionQmGateService.ComputePhysicalGrading(frame);

        Assert.Equal(first.Grade, second.Grade);
        Assert.Equal(first.ImpurityPct, second.ImpurityPct);
        Assert.Contains(first.Grade, new[] { "A", "B", "C" });
        Assert.InRange(first.ImpurityPct, 0m, 5m);
    }

    [Fact]
    public async Task VG02_PassingFrame_PostsGoodsReceiptAndInspectionResult()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var vision = CreateVisionMock(VisionSeverity.Pass);
        var service = CreateService(db, vision);

        var result = await service.ProcessFrameAsync(Frame(tenantId, FindPassingFrame(), "MAT-VG-01"));

        Assert.True(result.Passed, string.Join("; ", result.Errors));
        Assert.False(string.IsNullOrEmpty(result.MaterialDocumentNumber));
        Assert.False(string.IsNullOrEmpty(result.InspectionLotNumber));
        Assert.Empty(result.Errors);

        var header = await db.MaterialDocumentHeaders.AsNoTracking().SingleAsync(h => h.MovementType == 101);
        Assert.Equal(tenantId.ToString(), header.TenantId);
        Assert.Equal("VISION-GATE", header.RefDocument);

        var lot = await db.InspectionLots.AsNoTracking().SingleAsync(l => l.MaterialCode == "MAT-VG-01");
        Assert.Equal(tenantId, lot.TenantId);
        Assert.Equal("Created", lot.Status);

        var gateResult = await db.InspectionResults.AsNoTracking()
            .SingleAsync(r => r.Characteristic == GateCharacteristic);
        Assert.Equal(tenantId, gateResult.TenantId);
        Assert.Equal("Pass", gateResult.Evaluation);
        Assert.Equal("Passed", gateResult.Status);

        var audit = await db.AuditLogs.AsNoTracking().SingleAsync(a => a.ModuleName == "QM");
        Assert.Equal("VisionQmGate", audit.EntityName);
        Assert.Contains("passed=True", audit.Description);
    }

    [Fact]
    public async Task VG03_FailingFrame_CreatesNonConformanceAndNoGoodsReceipt()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var vision = CreateVisionMock(VisionSeverity.MajorDefect, VisionDefectType.ForeignParticle);
        var service = CreateService(db, vision);

        var result = await service.ProcessFrameAsync(Frame(tenantId, FindPassingFrame(), "MAT-VG-02"));

        Assert.False(result.Passed);
        Assert.True(string.IsNullOrEmpty(result.MaterialDocumentNumber));
        Assert.False(string.IsNullOrEmpty(result.NonConformanceId));
        Assert.Equal("MajorDefect", result.Severity);
        Assert.Equal(nameof(VisionDefectType.ForeignParticle), result.DefectType);

        Assert.Empty(db.MaterialDocumentHeaders);
        var nc = await db.NonConformances.AsNoTracking().SingleAsync();
        Assert.Equal("VISION-GATE", nc.DetectedBy);
        Assert.Equal("Major", nc.Severity);
        Assert.Equal("MAT-VG-02", nc.MaterialCode);

        var gateResult = await db.InspectionResults.AsNoTracking()
            .SingleAsync(r => r.Characteristic == GateCharacteristic);
        Assert.Equal("Fail", gateResult.Evaluation);
        Assert.Equal("Failed", gateResult.Status);
    }

    [Fact]
    public async Task VG04_InvalidBase64_ReturnsValidationErrorWithoutSideEffects()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var vision = CreateVisionMock(VisionSeverity.Pass);
        var service = CreateService(db, vision);

        var request = Frame(tenantId, Array.Empty<byte>(), "MAT-VG-03");
        request.ImageBase64 = "@@not-valid-base64@@";
        var result = await service.ProcessFrameAsync(request);

        Assert.False(result.Passed);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("base64"));
        Assert.Empty(db.InspectionResults);
        Assert.Empty(db.MaterialDocumentHeaders);
        Assert.Empty(db.NonConformances);
        vision.Verify(e => e.InspectImageAsync(It.IsAny<VisionInspectionRequest>()), Times.Never);
    }

    [Fact]
    public async Task VG05_GetRecentResults_FiltersByMaterialCode()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var vision = CreateVisionMock(VisionSeverity.Pass);
        var service = CreateService(db, vision);
        var frame = FindPassingFrame();

        await service.ProcessFrameAsync(Frame(tenantId, frame, "MAT-A"));
        await service.ProcessFrameAsync(Frame(tenantId, frame, "MAT-B"));

        var allRows = await service.GetRecentResultsAsync(tenantId, null, 1, 20);
        Assert.Equal(2, allRows.Count);

        var filtered = await service.GetRecentResultsAsync(tenantId, "MAT-A", 1, 20);
        Assert.Single(filtered);
        Assert.Contains("material=MAT-A", filtered[0].InspectorNotes);
        Assert.Equal(GateCharacteristic, filtered[0].Characteristic);

        var otherTenant = await service.GetRecentResultsAsync(Guid.NewGuid(), "MAT-A", 1, 20);
        Assert.Empty(otherTenant);
    }
}
