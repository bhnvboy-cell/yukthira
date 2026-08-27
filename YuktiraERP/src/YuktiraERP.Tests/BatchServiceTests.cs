using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class BatchServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task CreateBatchAsync_SetsIdAndStatus()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = new BatchEntity { BatchNumber = "BATCH-001", MaterialName = "Steel", Quantity = 100, UnitOfMeasure = "KG" };

        var result = await service.CreateBatchAsync(batch);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ACTIVE", result.Status);
        Assert.Equal(0, result.QuantityConsumed);
        Assert.True((DateTime.UtcNow - result.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task GetBatchAsync_ReturnsCorrectBatch()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-002", MaterialName = "Aluminum", Quantity = 50 });

        var result = await service.GetBatchAsync(batch.Id);

        Assert.NotNull(result);
        Assert.Equal("BATCH-002", result!.BatchNumber);
    }

    [Fact]
    public async Task GetBatchAsync_InvalidId_ReturnsNull()
    {
        var db = CreateDb();
        var service = new BatchService(db);

        var result = await service.GetBatchAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBatchByNumberAsync_ReturnsCorrectBatch()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-003", MaterialName = "Copper", Quantity = 200 });

        var result = await service.GetBatchByNumberAsync("BATCH-003");

        Assert.NotNull(result);
        Assert.Equal("Copper", result!.MaterialName);
    }

    [Fact]
    public async Task ExpireBatchAsync_ChangesStatusToExpired()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-004", MaterialName = "Zinc", Quantity = 75 });

        await service.ExpireBatchAsync(batch.Id);

        var refreshed = await service.GetBatchAsync(batch.Id);
        Assert.Equal("EXPIRED", refreshed!.Status);
    }

    [Fact]
    public async Task ExpireBatchAsync_NonActiveBatch_DoesNothing()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-005", MaterialName = "Tin", Quantity = 30 });
        batch.Status = "EXPIRED";
        await db.SaveChangesAsync();

        await service.ExpireBatchAsync(batch.Id);

        var refreshed = await service.GetBatchAsync(batch.Id);
        Assert.Equal("EXPIRED", refreshed!.Status);
    }

    [Fact]
    public async Task RecallBatchAsync_MarksBatchesAsRecalled()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var b1 = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-R1", MaterialName = "Iron", Quantity = 10 });
        var b2 = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-R2", MaterialName = "Iron", Quantity = 20 });
        var userId = Guid.NewGuid();

        var recall = await service.RecallBatchAsync("RECALL-001", new() { b1.Id, b2.Id }, "Quality issue", userId);

        Assert.Equal("RECALL-001", recall.RecallNumber);
        Assert.Equal("OPEN", recall.Status);
        Assert.Equal(userId, recall.InitiatedBy);

        var refreshed1 = await service.GetBatchAsync(b1.Id);
        var refreshed2 = await service.GetBatchAsync(b2.Id);
        Assert.Equal("RECALLED", refreshed1!.Status);
        Assert.Equal("RECALLED", refreshed2!.Status);
    }

    [Fact]
    public async Task RecordMovementAsync_ReceiptIncreasesQuantity()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-M1", MaterialName = "Steel", Quantity = 100 });

        var movement = await service.RecordMovementAsync(new BatchMovementEntity
        {
            BatchId = batch.Id,
            BatchNumber = batch.BatchNumber,
            MovementType = "RECEIPT",
            Quantity = 50,
            UserId = Guid.NewGuid()
        });

        Assert.NotEqual(Guid.Empty, movement.Id);
        var refreshed = await service.GetBatchAsync(batch.Id);
        Assert.Equal(150, refreshed!.Quantity);
    }

    [Fact]
    public async Task RecordMovementAsync_IssueDecreasesQuantity()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-M2", MaterialName = "Steel", Quantity = 100 });

        await service.RecordMovementAsync(new BatchMovementEntity
        {
            BatchId = batch.Id,
            BatchNumber = batch.BatchNumber,
            MovementType = "ISSUE",
            Quantity = 30,
            UserId = Guid.NewGuid()
        });

        var refreshed = await service.GetBatchAsync(batch.Id);
        Assert.Equal(70, refreshed!.Quantity);
        Assert.Equal(30, refreshed.QuantityConsumed);
    }

    [Fact]
    public async Task GetBatchTraceabilityAsync_ReturnsTraceData()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "BATCH-T1", MaterialName = "Steel", Quantity = 100 });

        await service.RecordMovementAsync(new BatchMovementEntity
        {
            BatchId = batch.Id, BatchNumber = batch.BatchNumber,
            MovementType = "RECEIPT", Quantity = 50, UserId = Guid.NewGuid()
        });
        await service.RecordMovementAsync(new BatchMovementEntity
        {
            BatchId = batch.Id, BatchNumber = batch.BatchNumber,
            MovementType = "ISSUE", Quantity = 20, UserId = Guid.NewGuid()
        });

        var trace = await service.GetBatchTraceabilityAsync(batch.Id);

        Assert.NotNull(trace.Batch);
        Assert.Single(trace.ForwardTrace);
        Assert.Single(trace.BackwardTrace);
    }

    [Fact]
    public async Task CheckExpiryAsync_ExpiresOverdueBatches()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity
        {
            BatchNumber = "BATCH-EXP",
            MaterialName = "Chemical",
            Quantity = 50,
            ExpiryDate = DateTime.UtcNow.AddDays(-5)
        });

        var expired = await service.CheckExpiryAsync();

        Assert.Single(expired);
        Assert.Equal(batch.Id, expired[0].Id);

        var refreshed = await service.GetBatchAsync(batch.Id);
        Assert.Equal("EXPIRED", refreshed!.Status);
    }

    [Fact]
    public async Task CheckExpiryAsync_DoesNotExpireFutureBatches()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        await service.CreateBatchAsync(new BatchEntity
        {
            BatchNumber = "BATCH-FUT",
            MaterialName = "Chemical",
            Quantity = 50,
            ExpiryDate = DateTime.UtcNow.AddDays(30)
        });

        var expired = await service.CheckExpiryAsync();

        Assert.Empty(expired);
    }

    [Fact]
    public async Task GenerateBatchCertificateAsync_ReturnsCertificateText()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity
        {
            BatchNumber = "BATCH-CERT",
            MaterialName = "Polymer",
            Quantity = 200,
            UnitOfMeasure = "KG",
            ManufacturingDate = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = DateTime.UtcNow.AddDays(335),
            SupplierName = "Acme Corp"
        });

        var cert = await service.GenerateBatchCertificateAsync(batch.Id);

        Assert.Contains("CERTIFICATE OF ANALYSIS", cert);
        Assert.Contains("BATCH-CERT", cert);
        Assert.Contains("Polymer", cert);
        Assert.Contains("Acme Corp", cert);
    }

    [Fact]
    public async Task GetAllBatchesAsync_ReturnsAll()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        await service.CreateBatchAsync(new BatchEntity { BatchNumber = "B1", MaterialName = "M1", Quantity = 10 });
        await service.CreateBatchAsync(new BatchEntity { BatchNumber = "B2", MaterialName = "M2", Quantity = 20 });

        var all = await service.GetAllBatchesAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateBatchAsync_UpdatesFields()
    {
        var db = CreateDb();
        var service = new BatchService(db);
        var batch = await service.CreateBatchAsync(new BatchEntity { BatchNumber = "B-UPD", MaterialName = "Orig", Quantity = 10 });

        batch.MaterialName = "Updated";
        batch.Quantity = 99;
        await service.UpdateBatchAsync(batch);

        var result = await service.GetBatchAsync(batch.Id);
        Assert.Equal("Updated", result!.MaterialName);
        Assert.Equal(99, result.Quantity);
    }
}
