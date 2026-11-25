using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Security;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Interceptors;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Tests;

public class CoreInfrastructureTests
{
    private class TestActorContext : ICurrentActorContext
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; } = Guid.NewGuid();
        public string? IpAddress { get; set; } = "192.168.1.100";
    }

    private static YuktiraDbContext CreateInMemoryDbContext(ICurrentActorContext actorContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(new AuditingSaveChangesInterceptor(actorContext))
            .Options;

        return new YuktiraDbContext(options);
    }

    // =========================================================================
    // 1. AUDITING & CREDENTIAL SCRUBBING TESTS
    // =========================================================================

    [Fact]
    public async Task AuditingInterceptor_OnEntityInsert_CapturesAuditLogWithActorAndScrubbedCredentials()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            Username = "financial_controller",
            Email = "controller@example.com",
            PasswordHash = "SuperSecretArgon2Hash$12345!"
        };

        db.IdentityUsers.Add(user);
        await db.SaveChangesAsync();

        // Verify IAuditable properties populated
        Assert.Equal(actor.UserId, user.CreatedByUserId);
        Assert.True(user.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1));

        // Verify AuditChangeLog created
        var auditLog = await db.AuditChangeLogs.FirstOrDefaultAsync(l => l.EntityPk == user.Id.ToString());
        Assert.NotNull(auditLog);
        Assert.Equal("IdentityUser", auditLog.EntityType);
        Assert.Equal("Insert", auditLog.Action);
        Assert.Equal(actor.UserId, auditLog.ActorUserId);
        Assert.Equal(actor.IpAddress, auditLog.IpAddress);

        // Verify Credential Scrubbing: PasswordHash must NOT leak into audit trail
        Assert.DoesNotContain("SuperSecretArgon2Hash", auditLog.ChangedFieldsJson);
        Assert.Contains("[REDACTED]", auditLog.ChangedFieldsJson);
    }

    [Fact]
    public async Task AuditingInterceptor_OnEntityUpdate_CapturesOnlyModifiedFields()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        var role = new IdentityRole
        {
            Id = Guid.NewGuid(),
            Code = "Role_Buyer",
            Name = "Junior Buyer",
            Description = "Initial purchasing role"
        };

        db.IdentityRoles.Add(role);
        await db.SaveChangesAsync();

        // Update only the Name property
        role.Name = "Senior Purchasing Officer";
        await db.SaveChangesAsync();

        var updateAuditLog = await db.AuditChangeLogs
            .Where(l => l.EntityPk == role.Id.ToString() && l.Action == "Update")
            .FirstOrDefaultAsync();

        Assert.NotNull(updateAuditLog);
        Assert.Contains("Senior Purchasing Officer", updateAuditLog.ChangedFieldsJson);
        Assert.Contains("Junior Buyer", updateAuditLog.ChangedFieldsJson);

        // Unmodified field (Description) must NOT be captured as changed
        Assert.DoesNotContain("Initial purchasing role", updateAuditLog.ChangedFieldsJson);
    }

    // =========================================================================
    // 2. SEGREGATION OF DUTIES (SoD) CONFLICT ENGINE TESTS
    // =========================================================================

    [Fact]
    public async Task SodConflictEngine_DetectsToxicCombination_SOD001_StrictBlock()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        var rbacMock = new Mock<IRbacService>();
        var targetUser = Guid.NewGuid();

        // User already has Vendor Maintenance permission
        rbacMock.Setup(r => r.GetUserRolesAsync(targetUser, null, default))
            .ReturnsAsync(new List<string> { "Role_Vendor_Admin" });
        rbacMock.Setup(r => r.GetUserPermissionsAsync(targetUser, null, default))
            .ReturnsAsync(new List<string> { "VendorMaster_Maintain" });

        var sodEngine = new SodConflictEngine(db, rbacMock.Object);

        // Attempt to assign conflicting AP Invoice Posting permission
        var result = await sodEngine.CheckRoleAssignmentConflictsAsync(targetUser, "APInvoice_Post");

        Assert.True(result.HasConflict);
        Assert.True(result.HasBlockingConflict);
        var violation = Assert.Single(result.Violations);
        Assert.Equal("SOD-001", violation.RuleCode);
        Assert.Equal(SodSeverity.Strict_Block, violation.Severity);
    }

    [Fact]
    public async Task SodConflictEngine_EnforcesMakerCheckerInvariant_SelfApprovalBlocked()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());
        var rbacMock = new Mock<IRbacService>();

        var sodEngine = new SodConflictEngine(db, rbacMock.Object);
        var creatorUserId = Guid.NewGuid();

        // Creator attempts to self-approve their own Purchase Order
        var result = await sodEngine.CheckActionExecutionConflictsAsync(
            userId: creatorUserId,
            documentType: "PurchaseOrder",
            proposedAction: "Approve",
            documentCreatorUserId: creatorUserId);

        Assert.True(result.HasConflict);
        Assert.True(result.HasBlockingConflict);
        Assert.Contains("Maker-Checker Invariant Violated", result.Violations[0].Message);
    }

    [Fact]
    public async Task SodConflictEngine_CleanAssignment_ReturnsNoConflicts()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());
        var rbacMock = new Mock<IRbacService>();
        var user = Guid.NewGuid();

        rbacMock.Setup(r => r.GetUserRolesAsync(user, null, default))
            .ReturnsAsync(new List<string> { "Role_Warehouse_Operator" });
        rbacMock.Setup(r => r.GetUserPermissionsAsync(user, null, default))
            .ReturnsAsync(new List<string> { "Bin_View" });

        var sodEngine = new SodConflictEngine(db, rbacMock.Object);

        var result = await sodEngine.CheckRoleAssignmentConflictsAsync(user, "GoodsReceipt_Post");

        Assert.False(result.HasConflict);
        Assert.Empty(result.Violations);
    }

    // =========================================================================
    // 3. DYNAMIC AUTHORIZATION & THRESHOLD RULES TESTS
    // =========================================================================

    [Fact]
    public async Task AuthorizationRuleService_AmountWithinThreshold_IsAuthorized()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        db.AuthorizationRules.Add(new AuthorizationRule
        {
            RuleId = "PO-TIER-01",
            DocumentType = "PurchaseOrder",
            MinThreshold = 10000m,
            MaxThreshold = 50000m,
            RequiredRole = "Role_Purchasing_Manager",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var authService = new AuthorizationRuleService(db);

        // Document is $25,000, and user holds Role_Purchasing_Manager
        var request = new AuthorizationEvaluationRequest
        {
            UserId = Guid.NewGuid(),
            DocumentType = "PurchaseOrder",
            DocumentId = Guid.NewGuid(),
            Amount = 25000m,
            UserRoles = new[] { "Role_Purchasing_Manager" }
        };

        var result = await authService.EvaluateDocumentAuthorizationAsync(request);

        Assert.True(result.IsAuthorized);
        Assert.False(result.RequiresHigherApproval);
    }

    [Fact]
    public async Task AuthorizationRuleService_AmountExceedsUserRoleLimit_RequiresEscalation()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        db.AuthorizationRules.Add(new AuthorizationRule
        {
            RuleId = "PO-TIER-02",
            DocumentType = "PurchaseOrder",
            MinThreshold = 50000m,
            MaxThreshold = 250000m,
            RequiredRole = "Role_VP_Finance",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var authService = new AuthorizationRuleService(db);

        // Document is $75,000, but user only has Role_Purchasing_Manager
        var request = new AuthorizationEvaluationRequest
        {
            UserId = Guid.NewGuid(),
            DocumentType = "PurchaseOrder",
            DocumentId = Guid.NewGuid(),
            Amount = 75000m,
            UserRoles = new[] { "Role_Purchasing_Manager" }
        };

        var result = await authService.EvaluateDocumentAuthorizationAsync(request);

        Assert.False(result.IsAuthorized);
        Assert.True(result.RequiresHigherApproval);
        Assert.Equal("Role_VP_Finance", result.RequiredRole);
    }

    [Fact]
    public async Task AuthorizationRuleService_BudgetExceeded_ForcesHigherApproval()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        db.AuthorizationRules.Add(new AuthorizationRule
        {
            RuleId = "PO-BUDGET-CHECK",
            DocumentType = "PurchaseOrder",
            MinThreshold = 5000m,
            MaxThreshold = 100000m,
            RequiredRole = "Role_CFO",
            RequireBudgetCheck = true,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var authService = new AuthorizationRuleService(db);

        var request = new AuthorizationEvaluationRequest
        {
            UserId = Guid.NewGuid(),
            DocumentType = "PurchaseOrder",
            DocumentId = Guid.NewGuid(),
            Amount = 8000m,
            IsBudgetExceeded = true,
            UserRoles = new[] { "Role_Purchasing_Manager" }
        };

        var result = await authService.EvaluateDocumentAuthorizationAsync(request);

        Assert.False(result.IsAuthorized);
        Assert.True(result.RequiresHigherApproval);
        Assert.True(result.BudgetCheckFailed);
        Assert.Equal("Role_CFO", result.RequiredRole);
    }

    // =========================================================================
    // 4. TRANSACTIONAL OUTBOX & EVENT PROCESSOR TESTS
    // =========================================================================

    public record TestStockIssuedEvent(Guid EventId, DateTime OccurredAtUtc, string ItemCode, decimal Qty) : IDomainEvent;

    [Fact]
    public async Task OutboxService_EnqueuesDomainEvent_AndProcessesSuccessfully()
    {
        var actor = new TestActorContext();
        using var db = CreateInMemoryDbContext(actor, Guid.NewGuid().ToString());

        var outboxService = new OutboxService(db);

        var domainEvent = new TestStockIssuedEvent(Guid.NewGuid(), DateTime.UtcNow, "RAW-MAT-001", 150m);
        await outboxService.EnqueueAsync(domainEvent, aggregateId: "RAW-MAT-001");

        // Verify message is persisted in outbox
        var pending = await outboxService.GetUnpublishedMessagesAsync(10);
        var msg = Assert.Single(pending);
        Assert.False(msg.IsPublished);
        Assert.Equal("RAW-MAT-001", msg.AggregateId);
        Assert.Contains("RAW-MAT-001", msg.PayloadJson);

        // Mark published
        await outboxService.MarkPublishedAsync(msg.Id);

        var afterPublish = await outboxService.GetUnpublishedMessagesAsync(10);
        Assert.Empty(afterPublish);

        var publishedRecord = await db.OutboxMessages.FindAsync(msg.Id);
        Assert.NotNull(publishedRecord);
        Assert.True(publishedRecord.IsPublished);
        Assert.NotNull(publishedRecord.PublishedAtUtc);
    }

    // =========================================================================
    // 5. MODULE LOADER & TOPOLOGICAL DEPENDENCY SORTING TESTS
    // =========================================================================

    private class MockModule : IYuktiraModule
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Version => "1.0.0";
        public int Priority { get; init; }
        public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

        public void RegisterServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
        public void ConfigureEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { }
        public void ConfigureModel(ModelBuilder modelBuilder) { }
        public void RegisterEventSubscriptions(IEventBusRegistry registry) { }
        public IReadOnlyList<NavigationRouteDescriptor> GetNavigationRoutes() => Array.Empty<NavigationRouteDescriptor>();
    }

    [Fact]
    public void ModuleRegistry_TopologicalSort_OrdersPrerequisitesFirst()
    {
        var registry = new ModuleRegistry();

        var modSales = new MockModule { Id = "sales", Name = "Sales Module", Priority = 50, Dependencies = new[] { "masterdata", "inventory" } };
        var modMasterData = new MockModule { Id = "masterdata", Name = "Master Data", Priority = 10, Dependencies = Array.Empty<string>() };
        var modInventory = new MockModule { Id = "inventory", Name = "Inventory", Priority = 20, Dependencies = new[] { "masterdata" } };

        // Register in arbitrary order
        registry.RegisterModule(modSales);
        registry.RegisterModule(modMasterData);
        registry.RegisterModule(modInventory);

        var ordered = registry.GetModulesInPriorityOrder();

        var indices = ordered.Select((m, i) => new { m.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index);

        // masterdata must precede inventory and sales
        Assert.True(indices["masterdata"] < indices["inventory"]);
        Assert.True(indices["masterdata"] < indices["sales"]);
        // inventory must precede sales
        Assert.True(indices["inventory"] < indices["sales"]);
    }

    [Fact]
    public void ModuleRegistry_CircularDependency_ThrowsInvalidOperationException()
    {
        var registry = new ModuleRegistry();

        var modA = new MockModule { Id = "mod-a", Name = "A", Priority = 1, Dependencies = new[] { "mod-b" } };
        var modB = new MockModule { Id = "mod-b", Name = "B", Priority = 2, Dependencies = new[] { "mod-a" } };

        registry.RegisterModule(modA);
        registry.RegisterModule(modB);

        Assert.Throws<InvalidOperationException>(() => registry.GetModulesInPriorityOrder());
    }
}
