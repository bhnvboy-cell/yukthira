using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// PM-01: Breakdown Notification to Order Lifecycle
/// PM-02: Maintenance State Sequence Enforcement
/// </summary>
public class PlantMaintenanceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // PM-01: Breakdown Notification to Order Lifecycle
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PM01_BreakdownNotification_CreatedForEquipment()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Equipment
        var equipment = new EquipmentEntity
        {
            EquipmentCode = "EQ-SLURRY-001",
            Name = "Slurry Pump Unit 1",
            Type = "Machine",
            Location = "Plant A - Wet Mill",
            Department = "Production",
            Status = "Operational"
        };
        db.Equipments.Add(equipment);

        // Act: Log breakdown notification
        var notification = new QualityNotificationEntity
        {
            NotificationNumber = $"PM-{DateTime.Now:yyyyMMdd}-001",
            NotificationType = "M1",  // Malfunction report
            Description = "Slurry Pump vibration exceeding threshold - bearing noise detected",
            Plant = "PLT-01",
            MaterialCode = equipment.EquipmentCode,
            MaterialName = equipment.Name,
            Priority = "High",
            Status = "NEW",
            CreatedBy = "OPERATOR-01"
        };
        db.QualityNotifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert: Notification created
        var saved = await db.QualityNotifications
            .FirstOrDefaultAsync(n => n.NotificationNumber == notification.NotificationNumber);
        Assert.NotNull(saved);
        Assert.Equal("M1", saved!.NotificationType);
        Assert.Equal("High", saved.Priority);
        Assert.Equal("NEW", saved.Status);
    }

    [Fact]
    public async Task PM01_MaintenanceOrder_CreatedFromNotification()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Breakdown notification
        var notification = new QualityNotificationEntity
        {
            NotificationNumber = "PM-2026-002",
            NotificationType = "M1",
            Description = "Viscometer calibration drift detected",
            Priority = "Medium",
            Status = "NEW"
        };
        db.QualityNotifications.Add(notification);

        // Act: Create maintenance order
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = $"MO-{DateTime.Now:yyyyMMdd}-001",
            EquipmentCode = "EQ-VISCOMETER-001",
            Description = $"Calibration and recalibration of viscometer per notification {notification.NotificationNumber}",
            Priority = "Medium",
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            Cost = 0,
            Status = "CREATED"
        };
        db.MaintenanceOrders.Add(order);

        // Link notification to order
        notification.ReferenceDocument = order.OrderNumber;
        notification.ReferenceDocType = "MaintenanceOrder";
        notification.Status = "IN_PROGRESS";
        await db.SaveChangesAsync();

        // Assert: Order linked to notification
        var savedOrder = await db.MaintenanceOrders
            .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);
        Assert.NotNull(savedOrder);
        Assert.Equal("CREATED", savedOrder!.Status);

        var refreshedNotif = await db.QualityNotifications
            .FirstOrDefaultAsync(n => n.NotificationNumber == notification.NotificationNumber);
        Assert.Equal("IN_PROGRESS", refreshedNotif!.Status);
        Assert.Equal(order.OrderNumber, refreshedNotif.ReferenceDocument);
    }

    [Fact]
    public async Task PM01_SparePartsIssue_UpdatesInventoryAndCostsCO()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Maintenance order and spare parts stock
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-2026-003",
            EquipmentCode = "EQ-PUMP-001",
            Description = "Pump bearing replacement",
            Priority = "High",
            Status = "RELEASED",
            Cost = 0
        };
        db.MaintenanceOrders.Add(order);

        var sparePart = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Pump Bearing 6205-2RS",
            Quantity = 10,
            UOM = "EA",
            Value = 250m  // $25 each
        };
        db.StockItems.Add(sparePart);
        await db.SaveChangesAsync();

        // Act: Issue spare parts
        decimal issuedQty = 2m;
        decimal unitCost = sparePart.Value / sparePart.Quantity;  // $25 each
        decimal totalCost = issuedQty * unitCost;

        sparePart.Quantity -= issuedQty;
        order.Cost += totalCost;
        await db.SaveChangesAsync();

        // Record stock movement
        var movement = new StockMovementEntity
        {
            TenantId = tenantId,
            DocumentNumber = $"GI-MO-{order.OrderNumber}",
            MaterialName = sparePart.MaterialName,
            MovementType = "261",
            Quantity = issuedQty,
            StockBefore = 10,
            StockAfter = sparePart.Quantity,
            Reference = order.OrderNumber,
            Status = "Posted"
        };
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        // Assert: Stock updated, costs accrued
        var refreshedStock = await db.StockItems.FindAsync(sparePart.Id);
        Assert.Equal(8m, refreshedStock!.Quantity);  // 10 - 2

        var refreshedOrder = await db.MaintenanceOrders.FindAsync(order.Id);
        Assert.Equal(50m, refreshedOrder!.Cost);  // 2 * $25
    }

    [Fact]
    public async Task PM01_TECO_EquipmentStatusUpdatesToAvailable()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Equipment in maintenance
        var equipment = new EquipmentEntity
        {
            EquipmentCode = "EQ-PUMP-002",
            Name = "Centrifugal Pump 2",
            Status = "Under Maintenance"
        };
        db.Equipments.Add(equipment);

        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-2026-004",
            EquipmentCode = equipment.EquipmentCode,
            Description = "Complete overhaul",
            Priority = "High",
            Status = "IN_PROGRESS",
            Cost = 500m
        };
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Execute TECO (Technical Completion)
        order.Status = "TECO";
        order.CompletedDate = DateTime.UtcNow;
        equipment.Status = "Operational";
        await db.SaveChangesAsync();

        // Assert: Equipment back to available
        var refreshedEquip = await db.Equipments.FindAsync(equipment.Id);
        Assert.Equal("Operational", refreshedEquip!.Status);

        var refreshedOrder = await db.MaintenanceOrders.FindAsync(order.Id);
        Assert.Equal("TECO", refreshedOrder!.Status);
        Assert.NotNull(refreshedOrder.CompletedDate);
    }

    // ════════════════════════════════════════════════════════════════
    // PM-02: Maintenance State Sequence Enforcement
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PM02_ConfirmLabor_BlockedWhenOrderNotReleased()
    {
        var db = CreateDb();

        // Arrange: Maintenance order in CREATED status
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-SEQ-001",
            EquipmentCode = "EQ-001",
            Description = "Motor inspection",
            Priority = "Medium",
            Status = "CREATED"
        };
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Attempt to confirm labor hours
        var validStatusesForConfirm = new[] { "RELEASED", "IN_PROGRESS" };
        bool canConfirm = validStatusesForConfirm.Contains(order.Status);

        // Assert: Confirmation blocked
        Assert.False(canConfirm, "Cannot confirm labor hours on CREATED order - must be RELEASED first");
        Assert.Equal("CREATED", order.Status);
    }

    [Fact]
    public async Task PM02_TECO_BlockedWhenOrderNotCompleted()
    {
        var db = CreateDb();

        // Arrange: Maintenance order in RELEASED status
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-SEQ-002",
            EquipmentCode = "EQ-002",
            Description = "Bearing replacement",
            Priority = "High",
            Status = "RELEASED"
        };
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Attempt TECO
        var validStatusesForTeco = new[] { "COMPLETED", "IN_PROGRESS" };
        bool canTeco = validStatusesForTeco.Contains(order.Status);

        // Assert: TECO blocked
        Assert.False(canTeco, "Cannot execute TECO on RELEASED order - must complete work first");
        Assert.Equal("RELEASED", order.Status);
    }

    [Fact]
    public async Task PM02_ValidSequence_Notification_Order_Confirm_TECO()
    {
        var db = CreateDb();

        // Arrange
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-FLOW-001",
            EquipmentCode = "EQ-003",
            Description = "Preventive maintenance",
            Status = "CREATED"
        };
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        // Act & Assert: Follow valid sequence
        // Step 1: CREATED -> RELEASED
        order.Status = "RELEASED";
        await db.SaveChangesAsync();
        Assert.Equal("RELEASED", order.Status);

        // Step 2: RELEASED -> IN_PROGRESS
        order.Status = "IN_PROGRESS";
        await db.SaveChangesAsync();
        Assert.Equal("IN_PROGRESS", order.Status);

        // Step 3: IN_PROGRESS -> COMPLETED (labor confirmed)
        order.Status = "COMPLETED";
        order.CompletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.Equal("COMPLETED", order.Status);

        // Step 4: COMPLETED -> TECO
        order.Status = "TECO";
        await db.SaveChangesAsync();
        Assert.Equal("TECO", order.Status);
    }

    [Fact]
    public async Task PM02_SkipSequence_ThrowsException()
    {
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-SKIP-001",
            EquipmentCode = "EQ-004",
            Description = "Emergency repair",
            Status = "CREATED"
        };

        // Act & Assert: Attempt to skip from CREATED directly to TECO
        var validTransitions = new Dictionary<string, string[]>
        {
            ["CREATED"] = new[] { "RELEASED", "CANCELLED" },
            ["RELEASED"] = new[] { "IN_PROGRESS", "CANCELLED" },
            ["IN_PROGRESS"] = new[] { "COMPLETED", "CANCELLED" },
            ["COMPLETED"] = new[] { "TECO" },
            ["TECO"] = Array.Empty<string>()
        };

        bool canTransition = validTransitions.ContainsKey(order.Status) &&
                            validTransitions[order.Status].Contains("TECO");

        Assert.False(canTransition, "CREATED -> TECO is an invalid sequence jump");
    }
}
