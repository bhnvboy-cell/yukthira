using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class UoM_MdG_QmTests
{
    // Helper to create InMemory DbContext
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new YuktiraDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private Mock<ITenantContext> CreateTenantContext(Guid tenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(t => t.TenantId).Returns(tenantId);
        return mock;
    }

    private Mock<IAuditService> CreateAuditService()
    {
        var mock = new Mock<IAuditService>();
        mock.Setup(a => a.LogAsync(It.IsAny<AuditEntryDto>())).Returns(Task.CompletedTask);
        return mock;
    }

    private static readonly Guid TestTenantId = Guid.NewGuid();

    // ==================== MODULE 1: UoM TESTS ====================

    [Fact]
    public async Task UoM_Convert_Kg_To_G()
    {
        using var db = CreateInMemoryDb();
        // Seed UoM: KG (Numerator=1000, Denominator=1), G (Numerator=1, Denominator=1000)
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity
        {
            TenantId = TestTenantId, Msehi = "KG", IsoCode = "KG",
            DimensionCode = "MASS", Numerator = 1, Denominator = 1, Decimals = 3
        });
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity
        {
            TenantId = TestTenantId, Msehi = "G", IsoCode = "G",
            DimensionCode = "MASS", Numerator = 1, Denominator = 1000, Decimals = 3
        });
        await db.SaveChangesAsync();

        var svc = new UomConversionService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var result = await svc.ConvertAsync(new UomConversionRequest
        {
            SourceValue = 1, SourceUomCode = "KG", TargetUomCode = "G"
        }, TestTenantId);

        Assert.True(result.Success);
        Assert.Equal(1000m, result.TargetValue);
    }

    [Fact]
    public async Task UoM_Convert_Temperature_Celsius_To_Fahrenheit()
    {
        using var db = CreateInMemoryDb();
        // C: Numerator=9, Denominator=5, AddOffset=32
        // F: Numerator=1, Denominator=1 (base)
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity
        {
            TenantId = TestTenantId, Msehi = "C", IsoCode = "CEL",
            DimensionCode = "TEMP", Numerator = 9, Denominator = 5, AddOffset = 32, Decimals = 1
        });
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity
        {
            TenantId = TestTenantId, Msehi = "F", IsoCode = "FAH",
            DimensionCode = "TEMP", Numerator = 1, Denominator = 1, Decimals = 1
        });
        await db.SaveChangesAsync();

        var svc = new UomConversionService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var result = await svc.ConvertAsync(new UomConversionRequest
        {
            SourceValue = 100, SourceUomCode = "C", TargetUomCode = "F"
        }, TestTenantId);

        Assert.True(result.Success);
        Assert.Equal(212m, result.TargetValue);
    }

    [Fact]
    public async Task UoM_ValidateDecimalPrecision()
    {
        using var db = CreateInMemoryDb();
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity
        {
            TenantId = TestTenantId, Msehi = "KG", IsoCode = "KG",
            DimensionCode = "MASS", Decimals = 2, Numerator = 1, Denominator = 1
        });
        await db.SaveChangesAsync();

        var svc = new UomConversionService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var result = svc.ValidateDecimalPrecision("KG", 1.2345m);

        Assert.False(result.IsValid);
        Assert.Equal(1.23m, result.RoundedValue);
    }

    [Fact]
    public async Task UoM_GetActiveUoms_FiltersByTenant()
    {
        using var db = CreateInMemoryDb();
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity { TenantId = TestTenantId, Msehi = "KG", IsoCode = "KG", DimensionCode = "MASS" });
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity { TenantId = Guid.NewGuid(), Msehi = "LB", IsoCode = "LB", DimensionCode = "MASS" });
        await db.SaveChangesAsync();

        var svc = new UomConversionService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var result = await svc.GetActiveUomsAsync(TestTenantId);

        Assert.Single(result);
        Assert.Equal("KG", result[0].Msehi);
    }

    // ==================== MODULE 2: MDG TESTS ====================

    [Fact]
    public async Task Mdg_SubmitChangeRequest_CreatesRequestWithPendingStatus()
    {
        using var db = CreateInMemoryDb();
        var svc = new MdgService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var result = await svc.SubmitChangeRequestAsync(new MdgSubmitRequest
        {
            EntityName = "MaterialMaster",
            StagingPayload = "{\"Code\":\"MAT-001\",\"Name\":\"Test Material\"}",
            RequestedBy = "admin"
        }, TestTenantId);

        Assert.NotNull(result);
        Assert.Equal("PendingApproval", result.Status);
        Assert.StartsWith("MDG-", result.RequestNumber);
    }

    [Fact]
    public async Task Mdg_ApproveChangeRequest_TransitionsStatus()
    {
        using var db = CreateInMemoryDb();
        var svc = new MdgService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var created = await svc.SubmitChangeRequestAsync(new MdgSubmitRequest
        {
            EntityName = "MaterialMaster",
            StagingPayload = "{\"Code\":\"MAT-002\",\"Name\":\"Approved Material\"}",
            RequestedBy = "admin"
        }, TestTenantId);

        var approved = await svc.ApproveChangeRequestAsync(created.Id, new MdgApprovalRequest
        {
            ApprovedBy = "manager",
            Notes = "Looks good"
        }, TestTenantId);

        Assert.Equal("Approved", approved.Status);
        Assert.Equal("manager", approved.ApprovedBy);
    }

    [Fact]
    public async Task Mdg_RejectChangeRequest_TransitionsToRejected()
    {
        using var db = CreateInMemoryDb();
        var svc = new MdgService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var created = await svc.SubmitChangeRequestAsync(new MdgSubmitRequest
        {
            EntityName = "Vendor",
            StagingPayload = "{\"Code\":\"V-001\",\"Name\":\"Bad Vendor\"}",
            RequestedBy = "admin"
        }, TestTenantId);

        var rejected = await svc.RejectChangeRequestAsync(created.Id, new MdgRejectionRequest
        {
            RejectedBy = "manager",
            Reason = "Incomplete data"
        }, TestTenantId);

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Incomplete data", rejected.RejectionReason);
    }

    [Fact]
    public async Task Mdg_ValidateStagingPayload_InvalidJson_ReturnsError()
    {
        using var db = CreateInMemoryDb();
        var svc = new MdgService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var result = await svc.ValidateStagingPayloadAsync("MaterialMaster", "not json", TestTenantId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("JSON"));
    }

    [Fact]
    public async Task Mdg_GetPendingRequests_FiltersByTenant()
    {
        using var db = CreateInMemoryDb();
        var svc = new MdgService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        await svc.SubmitChangeRequestAsync(new MdgSubmitRequest
        {
            EntityName = "MaterialMaster",
            StagingPayload = "{\"Code\":\"MAT-003\"}",
            RequestedBy = "admin"
        }, TestTenantId);

        var pending = await svc.GetPendingRequestsAsync(TestTenantId);
        Assert.Single(pending);
    }

    // ==================== MODULE 3: MIC TESTS ====================

    [Fact]
    public async Task Mic_CreateMIC_Success()
    {
        using var db = CreateInMemoryDb();
        var svc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var result = await svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000",
            CharacteristicCode = "MIC-001",
            IsQuantitative = true,
            ShortText = "Weight Check"
        }, TestTenantId);

        Assert.NotNull(result);
        Assert.Equal("MIC-001", result.CharacteristicCode);
        Assert.Equal("BeingCreated", result.Status);
    }

    [Fact]
    public async Task Mic_DuplicateDetection_ThrowsException()
    {
        using var db = CreateInMemoryDb();
        var svc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        await svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000",
            CharacteristicCode = "MIC-DUP",
            IsQuantitative = true,
            ShortText = "Duplicate Test"
        }, TestTenantId);

        await Assert.ThrowsAsync<DuplicateEntityException>(() => svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000",
            CharacteristicCode = "MIC-DUP",
            IsQuantitative = true,
            ShortText = "Should Fail"
        }, TestTenantId));
    }

    [Fact]
    public async Task Mic_CheckExists_ReturnsTrue_WhenExists()
    {
        using var db = CreateInMemoryDb();
        var svc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        await svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000",
            CharacteristicCode = "MIC-CHECK",
            IsQuantitative = false,
            ShortText = "Exists Check"
        }, TestTenantId);

        var exists = await svc.CheckMicExistsAsync("1000", "MIC-CHECK", TestTenantId);
        Assert.True(exists);
    }

    [Fact]
    public async Task Mic_CheckExists_ReturnsFalse_WhenNotExists()
    {
        using var db = CreateInMemoryDb();
        var svc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var exists = await svc.CheckMicExistsAsync("1000", "NONEXISTENT", TestTenantId);
        Assert.False(exists);
    }

    [Fact]
    public async Task Mic_Search_FiltersByStatus()
    {
        using var db = CreateInMemoryDb();
        var svc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var mic1 = await svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "MIC-S1", IsQuantitative = true, ShortText = "Released MIC"
        }, TestTenantId);
        var mic2 = await svc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "MIC-S2", IsQuantitative = true, ShortText = "Draft MIC"
        }, TestTenantId);

        var released = await svc.SearchMicsAsync(new MicSearchFilter { Status = "BeingCreated" }, TestTenantId);
        Assert.Equal(2, released.Count); // Both are BeingCreated by default
    }

    // ==================== MODULE 4: INSPECTION PLAN TESTS ====================

    [Fact]
    public async Task Plan_CreateGenericPlan_CreatesWithDefaults()
    {
        using var db = CreateInMemoryDb();
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var plan = await planSvc.CreateGenericPlanAsync(new InspectionPlanCreateRequest
        {
            PlantId = "1000",
            MaterialId = "MAT-GEN"
        }, TestTenantId);

        Assert.NotNull(plan);
        Assert.Equal("5", plan.Usage);
        Assert.Equal("4", plan.OverallStatus);
        Assert.Single(plan.Operations); // Default "0010"
    }

    [Fact]
    public async Task Plan_Resolve_SpecificMatch_First()
    {
        using var db = CreateInMemoryDb();
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        // Create specific plan
        var specific = await planSvc.CreateGenericPlanAsync(new InspectionPlanCreateRequest
        {
            PlantId = "1000", MaterialId = "MAT-SPECIFIC"
        }, TestTenantId);

        // Resolve
        var resolved = await planSvc.ResolvePlanAsync(new InspectionPlanResolveRequest
        {
            PlantId = "1000", MaterialId = "MAT-SPECIFIC"
        }, TestTenantId);

        Assert.NotNull(resolved);
        Assert.Equal(specific.Id, resolved!.Id);
    }

    [Fact]
    public async Task Plan_Resolve_FallbackToGeneric_WhenNoSpecific()
    {
        using var db = CreateInMemoryDb();
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        // Create generic plan (MaterialId = null)
        await planSvc.CreateGenericPlanAsync(new InspectionPlanCreateRequest
        {
            PlantId = "1000", MaterialId = null
        }, TestTenantId);

        // Resolve for a specific material - should fallback
        var resolved = await planSvc.ResolvePlanAsync(new InspectionPlanResolveRequest
        {
            PlantId = "1000", MaterialId = "MAT-ANY"
        }, TestTenantId);

        Assert.NotNull(resolved);
        Assert.Null(resolved!.MaterialId); // It's the generic plan
    }

    [Fact]
    public async Task Plan_Resolve_ReturnsNull_WhenNoneExist()
    {
        using var db = CreateInMemoryDb();
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        var resolved = await planSvc.ResolvePlanAsync(new InspectionPlanResolveRequest
        {
            PlantId = "9999", MaterialId = "NO-MATCH"
        }, TestTenantId);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Plan_AssignMicToOperation_Success()
    {
        using var db = CreateInMemoryDb();
        var micSvc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);

        // Create MIC
        await micSvc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "MIC-PLAN-001", IsQuantitative = true, ShortText = "Plan MIC"
        }, TestTenantId);

        // Create plan
        var plan = await planSvc.CreateGenericPlanAsync(new InspectionPlanCreateRequest
        {
            PlantId = "1000"
        }, TestTenantId);

        // Assign MIC to operation
        var opId = plan.Operations[0].Id;
        var updated = await planSvc.AssignMicToOperationAsync(plan.Id, new InspectionPlanMicAssignRequest
        {
            OperationId = opId,
            CharacteristicNo = 1,
            MicCode = "MIC-PLAN-001",
            MicPlantId = "1000",
            IsQuantitative = true,
            ShortText = "Plan MIC"
        }, TestTenantId);

        Assert.NotNull(updated);
        Assert.Single(updated.Operations[0].Characteristics);
    }

    // ==================== MODULE 5: AUTO-GENERATION TESTS ====================

    [Fact]
    public async Task AutoGen_GeneratePlan_CreatesPlanWithReleasedMics()
    {
        using var db = CreateInMemoryDb();
        var micSvc = new MicService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var autoGen = new AutoInspectionPlanGenerator(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object, planSvc);

        // Seed released MICs
        await micSvc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "AUTO-MIC-001", IsQuantitative = true, ShortText = "Weight Check"
        }, TestTenantId);
        await micSvc.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "AUTO-MIC-002", IsQuantitative = true, ShortText = "Temperature Check"
        }, TestTenantId);

        // Auto-generate
        var plan = await autoGen.GeneratePlanAsync(new AutoGenerationRequest
        {
            PlantId = "1000", MaterialId = "MAT-AUTO"
        }, TestTenantId);

        Assert.NotNull(plan);
        Assert.Equal("5", plan.Usage);
        Assert.Equal("4", plan.OverallStatus);
        Assert.NotNull(plan.Operations);
    }

    [Fact]
    public async Task AutoGen_SetsCorrectDefaults()
    {
        using var db = CreateInMemoryDb();
        var planSvc = new InspectionPlanService(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object);
        var autoGen = new AutoInspectionPlanGenerator(db, CreateTenantContext(TestTenantId).Object, CreateAuditService().Object, planSvc);

        var plan = await autoGen.GeneratePlanAsync(new AutoGenerationRequest
        {
            PlantId = "2000", MaterialId = "MAT-DEFAULTS"
        }, TestTenantId);

        Assert.Equal("5", plan.Usage);
        Assert.Equal("4", plan.OverallStatus);
        Assert.Equal(1, plan.Operations.Count);
        Assert.Equal("0010", plan.Operations[0].OperationNo);
    }

    // ==================== TENANT ISOLATION TESTS ====================

    [Fact]
    public async Task TenantIsolation_UoM_TwoTenantsSeparate()
    {
        using var db = CreateInMemoryDb();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity { TenantId = tenant1, Msehi = "KG", IsoCode = "KG", DimensionCode = "MASS" });
        db.UnitsOfMeasure.Add(new UnitOfMeasureEntity { TenantId = tenant2, Msehi = "LB", IsoCode = "LB", DimensionCode = "MASS" });
        await db.SaveChangesAsync();

        var svc1 = new UomConversionService(db, CreateTenantContext(tenant1).Object, CreateAuditService().Object);
        var svc2 = new UomConversionService(db, CreateTenantContext(tenant2).Object, CreateAuditService().Object);

        var uoms1 = await svc1.GetActiveUomsAsync(tenant1);
        var uoms2 = await svc2.GetActiveUomsAsync(tenant2);

        Assert.Single(uoms1);
        Assert.Equal("KG", uoms1[0].Msehi);
        Assert.Single(uoms2);
        Assert.Equal("LB", uoms2[0].Msehi);
    }

    [Fact]
    public async Task TenantIsolation_MIC_TwoTenantsSeparate()
    {
        using var db = CreateInMemoryDb();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var svc1 = new MicService(db, CreateTenantContext(tenant1).Object, CreateAuditService().Object);
        var svc2 = new MicService(db, CreateTenantContext(tenant2).Object, CreateAuditService().Object);

        await svc1.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "T1-MIC", IsQuantitative = true, ShortText = "T1"
        }, tenant1);
        await svc2.CreateMicAsync(new MicCreateRequest
        {
            PlantId = "1000", CharacteristicCode = "T2-MIC", IsQuantitative = true, ShortText = "T2"
        }, tenant2);

        var search1 = await svc1.SearchMicsAsync(new MicSearchFilter(), tenant1);
        var search2 = await svc2.SearchMicsAsync(new MicSearchFilter(), tenant2);

        Assert.Single(search1);
        Assert.Equal("T1-MIC", search1[0].CharacteristicCode);
        Assert.Single(search2);
        Assert.Equal("T2-MIC", search2[0].CharacteristicCode);
    }

    [Fact]
    public async Task TenantIsolation_InspectionPlan_Separate()
    {
        using var db = CreateInMemoryDb();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var svc1 = new InspectionPlanService(db, CreateTenantContext(tenant1).Object, CreateAuditService().Object);
        var svc2 = new InspectionPlanService(db, CreateTenantContext(tenant2).Object, CreateAuditService().Object);

        await svc1.CreateGenericPlanAsync(new InspectionPlanCreateRequest { PlantId = "1000" }, tenant1);
        await svc2.CreateGenericPlanAsync(new InspectionPlanCreateRequest { PlantId = "1000" }, tenant2);

        var plans1 = await svc1.GetPlansAsync(tenant1);
        var plans2 = await svc2.GetPlansAsync(tenant2);

        Assert.Single(plans1);
        Assert.Single(plans2);
    }
}
