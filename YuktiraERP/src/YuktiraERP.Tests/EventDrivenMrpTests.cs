using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class EventDrivenMrpTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private static EventDrivenMrpEngine CreateEngine(YuktiraDbContext db, MrpEngineOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IInventoryService>(new InventoryService(db));
        services.AddSingleton<INumberRangeService>(new NumberRangeService(db));
        services.AddSingleton<IAuditService>(new AuditService(db));
        var provider = services.BuildServiceProvider();

        return new EventDrivenMrpEngine(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options ?? new MrpEngineOptions { Enabled = true }));
    }

    private static MrpDomainEvent CreateEvent(Guid tenantId, string materialCode, decimal quantity, string referenceId, string eventType = "order.created")
    {
        return new MrpDomainEvent
        {
            EventType = eventType,
            TenantId = tenantId,
            OccurredAt = DateTime.UtcNow,
            MaterialCodes = new List<string> { materialCode },
            Quantities = new Dictionary<string, decimal> { [materialCode] = quantity },
            ReferenceId = referenceId
        };
    }

    [Fact]
    public async Task MR01_ShortageEvent_CreatesDraftPurchaseOrder()
    {
        var db = CreateDb();
        db.MaterialMasters.Add(new MaterialMasterEntity
        {
            Code = "RM-STEEL",
            Name = "Steel Bar",
            Type = "RAW",
            UOM = "KG",
            Stock = 10,
            Price = 50,
            Status = "Active"
        });
        db.Vendors.Add(new VendorEntity { Code = "V-001", Name = "Acme Metals", Status = "Active", PaymentTerms = "Net 45" });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var tenantId = Guid.NewGuid();

        var result = await engine.HandleAsync(CreateEvent(tenantId, "RM-STEEL", 40, "SO-1001"));

        Assert.Equal(1, result.EvaluatedMaterials);
        var shortage = Assert.Single(result.Shortages);
        Assert.Equal("RM-STEEL", shortage.MaterialCode);
        Assert.Equal(40, shortage.RequiredQuantity);
        Assert.Equal(10, shortage.AvailableQuantity);
        Assert.Equal(30, shortage.ShortageQuantity);
        Assert.Equal("PURCHASE", shortage.ProcurementType);
        Assert.Equal("DRAFT_PO", shortage.ActionTaken);

        var poNumber = Assert.Single(result.DraftPurchaseOrders);
        Assert.StartsWith("PO", poNumber);

        var purchaseOrder = await db.PurchaseOrders.SingleAsync(p => p.PoNumber == poNumber);
        Assert.Equal("DRAFT", purchaseOrder.Status);
        Assert.Equal(tenantId, purchaseOrder.TenantId);
        Assert.Equal("Acme Metals", purchaseOrder.VendorName);
        Assert.Equal("Net 45", purchaseOrder.PaymentTerms);
        Assert.Equal(1500, purchaseOrder.TotalAmount);

        var item = await db.PurchaseOrderItems.SingleAsync();
        Assert.Equal("RM-STEEL", item.MaterialCode);
        Assert.Equal(30, item.Quantity);
        Assert.Equal("MRP:SO-1001", item.BatchNo);
        Assert.Equal("OPEN", item.Status);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.ModuleName == "MRP");
        Assert.Equal("PurchaseOrder", auditEntry.EntityName);
        Assert.Equal("Create", auditEntry.ActionType);
    }

    [Fact]
    public async Task MR02_ReplayedEvent_DoesNotDuplicateDraftPurchaseOrder()
    {
        var db = CreateDb();
        db.MaterialMasters.Add(new MaterialMasterEntity
        {
            Code = "RM-STEEL",
            Name = "Steel Bar",
            Type = "RAW",
            UOM = "KG",
            Stock = 0,
            Price = 50,
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var tenantId = Guid.NewGuid();
        var evt = CreateEvent(tenantId, "RM-STEEL", 10, "SO-2001");

        var first = await engine.HandleAsync(evt);
        var second = await engine.HandleAsync(evt);

        Assert.Single(first.DraftPurchaseOrders);
        Assert.Empty(second.DraftPurchaseOrders);
        Assert.Contains("RM-STEEL:duplicate-draft-po", second.SkippedReasons);
        Assert.Single(second.Shortages);
        Assert.Equal("SKIPPED", second.Shortages[0].ActionTaken);
        Assert.Equal(1, await db.PurchaseOrders.CountAsync());
        Assert.Equal(1, await db.PurchaseOrderItems.CountAsync());
    }

    [Fact]
    public async Task MR03_InHouseMaterial_CreatesDraftProductionOrder()
    {
        var db = CreateDb();
        db.MaterialMasters.Add(new MaterialMasterEntity
        {
            Code = "FG-WIDGET",
            Name = "Widget",
            Type = "FERT",
            UOM = "EA",
            Stock = 0,
            Price = 100,
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var tenantId = Guid.NewGuid();
        var evt = CreateEvent(tenantId, "FG-WIDGET", 25, "SO-3001");

        var result = await engine.HandleAsync(evt);

        var shortage = Assert.Single(result.Shortages);
        Assert.Equal("PRODUCTION", shortage.ProcurementType);
        Assert.Equal("DRAFT_PRODUCTION_ORDER", shortage.ActionTaken);

        var orderNumber = Assert.Single(result.DraftProductionOrders);
        Assert.StartsWith("PRD", orderNumber);

        var productionOrder = await db.ProductionOrders.SingleAsync(p => p.OrderNumber == orderNumber);
        Assert.Equal("PLANNED", productionOrder.Status);
        Assert.Equal("PP01", productionOrder.OrderType);
        Assert.Equal(tenantId, productionOrder.TenantId);
        Assert.Equal("FG-WIDGET", productionOrder.MaterialCode);
        Assert.Equal(25, productionOrder.Quantity);
        Assert.Equal("MRP:SO-3001", productionOrder.BatchNo);
        Assert.True(productionOrder.EndDate > productionOrder.StartDate);
        Assert.Empty(await db.PurchaseOrders.ToListAsync());

        var second = await engine.HandleAsync(evt);
        Assert.Empty(second.DraftProductionOrders);
        Assert.Contains("FG-WIDGET:duplicate-draft-production-order", second.SkippedReasons);
        Assert.Equal(1, await db.ProductionOrders.CountAsync());
    }

    [Fact]
    public async Task MR04_SufficientStock_CreatesNoDrafts()
    {
        var db = CreateDb();
        db.MaterialMasters.Add(new MaterialMasterEntity
        {
            Code = "RM-PLENTY",
            Name = "Plenty Material",
            Type = "RAW",
            UOM = "EA",
            Stock = 100,
            Price = 5,
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var tenantId = Guid.NewGuid();

        var result = await engine.HandleAsync(CreateEvent(tenantId, "RM-PLENTY", 10, "SO-4001"));

        Assert.Equal(1, result.EvaluatedMaterials);
        Assert.Empty(result.Shortages);
        Assert.Empty(result.DraftPurchaseOrders);
        Assert.Empty(result.DraftProductionOrders);
        Assert.Contains("RM-PLENTY:sufficient-stock", result.SkippedReasons);
        Assert.Equal(1, result.Totals.SkippedMaterials);
        Assert.Empty(await db.PurchaseOrders.ToListAsync());
        Assert.Empty(await db.ProductionOrders.ToListAsync());
    }
}
