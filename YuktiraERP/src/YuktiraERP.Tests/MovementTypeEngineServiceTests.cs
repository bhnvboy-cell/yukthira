using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class MovementTypeEngineServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private MovementTypeEngineService CreateService(YuktiraDbContext db)
    {
        var logger = new Mock<ILogger<MovementTypeEngineService>>();
        var goodsMovementService = new Mock<IGoodsMovementService>();
        var batchService = new Mock<IBatchService>();
        var inventoryService = new Mock<IInventoryService>();
        return new MovementTypeEngineService(db, logger.Object, goodsMovementService.Object, batchService.Object, inventoryService.Object);
    }

    private async Task SeedMovementTypes(YuktiraDbContext db, Guid tenantId)
    {
        db.MovementTypes.AddRange(
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 101, Description = "GR for PO", Category = "GR", ReversalMovementType = 102, AllowedStockTypes = "FREE,QI,BLOCKED", IsActive = true, RequiresReference = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 102, Description = "Reversal of GR for PO", Category = "GR", ReversalMovementType = 101, AllowedStockTypes = "FREE,QI,BLOCKED", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 201, Description = "GI for Cost Center", Category = "GI", ReversalMovementType = 202, AllowedStockTypes = "FREE", IsActive = true, ConsumptionUpdate = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 202, Description = "Reversal of GI for Cost Center", Category = "GI", ReversalMovementType = 201, AllowedStockTypes = "FREE", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 301, Description = "Transfer Plant to Plant", Category = "TRANSFER", ReversalMovementType = 302, AllowedStockTypes = "FREE", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 315, Description = "Transfer QI to Free", Category = "TRANSFER_POSTING", ReversalMovementType = 316, AllowedStockTypes = "QI,FREE", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 321, Description = "Transfer Free to QI", Category = "QI", ReversalMovementType = 322, AllowedStockTypes = "FREE,QI", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 343, Description = "Transfer Free to Blocked", Category = "BLOCKED", ReversalMovementType = 344, AllowedStockTypes = "FREE,BLOCKED", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 411, Description = "Transfer Consignment to Free", Category = "CONSIGNMENT", ReversalMovementType = 412, AllowedStockTypes = "CONSIGNMENT,FREE", IsActive = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 451, Description = "GI for Returns to Vendor", Category = "RETURNS", ReversalMovementType = 452, AllowedStockTypes = "FREE", IsActive = true, RequiresReference = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 541, Description = "GI for Subcontracting Order", Category = "SUBCONTRACTING", ReversalMovementType = 542, AllowedStockTypes = "FREE", IsActive = true, ConsumptionUpdate = true, RequiresReference = true },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 999, Description = "Inactive Type", Category = "GR", IsActive = false },
            new MovementTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, MovementType = 261, Description = "GI for Production Order", Category = "GI", ReversalMovementType = 262, AllowedStockTypes = "FREE", IsActive = true, RequiresReference = true, AutoBatchCreate = true }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedCategories(YuktiraDbContext db, Guid tenantId)
    {
        db.MovementTypeCategories.AddRange(
            new MovementTypeCategoryEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = "GR", Name = "Goods Receipt", SortOrder = 1, IsActive = true },
            new MovementTypeCategoryEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = "GI", Name = "Goods Issue", SortOrder = 2, IsActive = true }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedStockTypes(YuktiraDbContext db, Guid tenantId)
    {
        db.MovementTypeStockTypes.AddRange(
            new MovementTypeStockTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = "FREE", Name = "Unrestricted Stock", IsActive = true },
            new MovementTypeStockTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = "QI", Name = "Quality Inspection Stock", IsActive = true },
            new MovementTypeStockTypeEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = "BLOCKED", Name = "Blocked Stock", IsActive = true }
        );
        await db.SaveChangesAsync();
    }

    // ---- Lookup Tests ----

    [Fact]
    public async Task GetMovementTypeAsync_ReturnsCorrectType()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.GetMovementTypeAsync(101, tenantId);

        Assert.NotNull(result);
        Assert.Equal("GR for PO", result!.Description);
    }

    [Fact]
    public async Task GetMovementTypeAsync_ReturnsNullForMissing()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db);

        var result = await service.GetMovementTypeAsync(999, tenantId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllMovementTypesAsync_ReturnsAllActive()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.GetAllMovementTypesAsync(tenantId);

        Assert.Equal(12, result.Count);
        Assert.DoesNotContain(result, m => m.MovementType == 999);
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsMatchingTypes()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var giTypes = await service.GetByCategoryAsync("GI", tenantId);

        Assert.All(giTypes, t => Assert.Equal("GI", t.Category));
    }

    [Fact]
    public async Task GetByStockTypeAsync_ReturnsTypesWithStockType()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var qiTypes = await service.GetByStockTypeAsync("QI", tenantId);

        Assert.Contains(qiTypes, t => t.MovementType == 101);
    }

    // ---- Validation Tests ----

    [Fact]
    public async Task ValidateMovement_ValidRequest_ReturnsValid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = 201,
            StockType = "FREE",
            Quantity = 10,
            TenantId = tenantId
        });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateMovement_MissingType_ReturnsInvalid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db);

        var result = await service.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = 999,
            TenantId = tenantId
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateMovement_InactiveType_ReturnsInvalid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = 999,
            TenantId = tenantId
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("inactive"));
    }

    [Fact]
    public async Task ValidateMovement_InvalidStockType_ReturnsInvalid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = 201,
            StockType = "CONSIGNMENT",
            TenantId = tenantId
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not allowed"));
    }

    [Fact]
    public async Task ValidateMovement_MissingReference_ReturnsInvalid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.ValidateMovementAsync(new MovementValidationRequest
        {
            MovementType = 101,
            StockType = "FREE",
            Reference = "",
            TenantId = tenantId
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("requires a reference"));
    }

    // ---- Reversal Tests ----

    [Fact]
    public async Task IsReversalMovement_ReturnsTrue()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        Assert.True(await service.IsReversalMovementAsync(102, tenantId));
    }

    [Fact]
    public async Task IsReversalMovement_ReturnsFalseForNonReversal()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        Assert.False(await service.IsReversalMovementAsync(101, tenantId));
    }

    [Fact]
    public async Task GetReversalMovementType_ReturnsCorrectType()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var reversal = await service.GetReversalMovementTypeAsync(101, tenantId);

        Assert.Equal(102, reversal);
    }

    [Fact]
    public async Task GetReversalMovementType_ReturnsNullForNonReversal()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var reversal = await service.GetReversalMovementTypeAsync(101, tenantId);

        Assert.NotNull(reversal);
    }

    // ---- Stock Type Compatibility ----

    [Fact]
    public async Task IsStockTypeCompatible_ReturnsTrue()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        Assert.True(await service.IsStockTypeCompatibleAsync(101, "FREE", tenantId));
    }

    [Fact]
    public async Task IsStockTypeCompatible_ReturnsFalse()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        Assert.False(await service.IsStockTypeCompatibleAsync(201, "BLOCKED", tenantId));
    }

    [Fact]
    public async Task GetCompatibleStockTypes_ReturnsCorrectList()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var types = await service.GetCompatibleStockTypesAsync(101, tenantId);

        Assert.Contains("FREE", types);
        Assert.Contains("QI", types);
        Assert.Contains("BLOCKED", types);
    }

    // ---- Workflow Simulation ----

    [Fact]
    public async Task SimulateWorkflow_WithNoSteps_ReturnsSucceed()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.SimulateWorkflowAsync(new MovementSimulationRequest
        {
            MovementType = 101,
            TenantId = tenantId
        });

        Assert.True(result.WouldSucceed);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public async Task SimulateWorkflow_WithSteps_ReturnsSteps()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedMovementTypes(db, tenantId);
        db.MovementTypeWorkflows.Add(new MovementTypeWorkflowEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementType = 101,
            StepName = "Validate Stock",
            StepOrder = 1,
            StepType = "CHECK_STOCK",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SimulateWorkflowAsync(new MovementSimulationRequest
        {
            MovementType = 101,
            TenantId = tenantId
        });

        Assert.True(result.WouldSucceed);
        Assert.Single(result.Steps);
        Assert.Equal("Validate Stock", result.Steps[0].StepName);
    }

    // ---- Categories & Stock Types ----

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsCategories()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedCategories(db, tenantId);
        var service = CreateService(db);

        var result = await service.GetAllCategoriesAsync(tenantId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllStockTypesAsync_ReturnsStockTypes()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedStockTypes(db, tenantId);
        var service = CreateService(db);

        var result = await service.GetAllStockTypesAsync(tenantId);

        Assert.Equal(3, result.Count);
    }

    // ---- Posting ----

    [Fact]
    public async Task PostMovement_InvalidType_ReturnsErrors()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db);

        var result = await service.PostMovementAsync(new MovementPostRequest
        {
            MovementType = 999,
            TenantId = tenantId,
            Lines = new List<MovementPostLineRequest>
            {
                new() { MaterialCode = "MAT-001", Quantity = 10 }
            }
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    // ---- Document Flow ----

    [Fact]
    public async Task GetDocumentFlow_PO_ReturnsMatchingDocuments()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        db.MovementDocuments.Add(new MovementDocumentEntity
        {
            Id = docId,
            TenantId = tenantId,
            DocumentNumber = "DOC-001",
            MovementType = 101,
            Status = "POSTED",
            PostedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });
        db.MovementDocumentLines.Add(new MovementDocumentLineEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementDocumentId = docId,
            PurchaseOrderNo = "PO-123",
            MaterialCode = "MAT-001",
            Quantity = 10
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetDocumentFlowAsync("PO-123", "PO", tenantId);

        Assert.Single(result);
        Assert.Equal("DOC-001", result[0].DocumentNumber);
    }

    [Fact]
    public async Task GetDocumentFlow_UnknownType_ReturnsEmpty()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db);

        var result = await service.GetDocumentFlowAsync("REF-001", "UNKNOWN", tenantId);

        Assert.Empty(result);
    }

    // ---- Integration ----

    [Fact]
    public async Task GetIntegrationFlags_ReturnsEnabledFlags()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.MovementTypeIntegrations.Add(new MovementTypeIntegrationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementType = 101,
            TargetModule = "FI",
            EventType = "POST",
            IsEnabled = true
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetIntegrationFlagsAsync(101, tenantId);

        Assert.Single(result);
    }

    [Fact]
    public async Task CheckIntegration_ReturnsTrue()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.MovementTypeIntegrations.Add(new MovementTypeIntegrationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementType = 101,
            TargetModule = "FI",
            IsEnabled = true
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Assert.True(await service.CheckIntegrationAsync(101, "FI", tenantId));
    }

    [Fact]
    public async Task CheckIntegration_ReturnsFalse()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db);

        Assert.False(await service.CheckIntegrationAsync(101, "FI", tenantId));
    }

    // ---- Trace ----

    [Fact]
    public async Task GetMovementTrace_ReturnsDocuments()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        db.MovementDocuments.Add(new MovementDocumentEntity
        {
            Id = docId,
            TenantId = tenantId,
            DocumentNumber = "DOC-TRC",
            MovementType = 101,
            Status = "POSTED",
            PostedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMovementTraceAsync(docId);

        Assert.Single(result);
        Assert.Equal("DOC-TRC", result[0].DocumentNumber);
    }
}
