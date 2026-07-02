using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data;

public class YuktiraDbContext : DbContext
{
    public Guid? TenantId { get; set; }

    public YuktiraDbContext(DbContextOptions<YuktiraDbContext> options) : base(options) { }

    // MM
    public DbSet<MaterialMasterEntity> MaterialMasters => Set<MaterialMasterEntity>();
    public DbSet<VendorEntity> Vendors => Set<VendorEntity>();
    public DbSet<PurchaseRequisitionEntity> PurchaseRequisitions => Set<PurchaseRequisitionEntity>();
    public DbSet<PurchaseOrderEntity> PurchaseOrders => Set<PurchaseOrderEntity>();
    public DbSet<GoodsReceiptEntity> GoodsReceipts => Set<GoodsReceiptEntity>();
    public DbSet<StockItemEntity> StockItems => Set<StockItemEntity>();
    public DbSet<InvoiceVerificationEntity> InvoiceVerifications => Set<InvoiceVerificationEntity>();
    // SD
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<SalesOrderEntity> SalesOrders => Set<SalesOrderEntity>();
    public DbSet<SalesOrderLineEntity> SalesOrderLines => Set<SalesOrderLineEntity>();
    public DbSet<InquiryEntity> Inquiries => Set<InquiryEntity>();
    public DbSet<QuotationEntity> Quotations => Set<QuotationEntity>();
    public DbSet<DeliveryEntity> Deliveries => Set<DeliveryEntity>();
    public DbSet<BillingDocumentEntity> BillingDocuments => Set<BillingDocumentEntity>();
    // PP
    public DbSet<ProductionPlanEntity> ProductionPlans => Set<ProductionPlanEntity>();
    public DbSet<BillOfMaterialEntity> BillOfMaterials => Set<BillOfMaterialEntity>();
    public DbSet<ProductionRoutingEntity> ProductionRoutings => Set<ProductionRoutingEntity>();
    public DbSet<WorkCenterEntity> WorkCenters => Set<WorkCenterEntity>();
    public DbSet<ProductionOrderEntity> ProductionOrders => Set<ProductionOrderEntity>();
    // QM
    public DbSet<InspectionLotEntity> InspectionLots => Set<InspectionLotEntity>();
    public DbSet<InspectionPlanEntity> InspectionPlans => Set<InspectionPlanEntity>();
    public DbSet<InspectionResultEntity> InspectionResults => Set<InspectionResultEntity>();
    public DbSet<UsageDecisionEntity> UsageDecisions => Set<UsageDecisionEntity>();
    // WM
    public DbSet<WarehouseTransferEntity> WarehouseTransfers => Set<WarehouseTransferEntity>();
    public DbSet<StorageLocationEntity> StorageLocations => Set<StorageLocationEntity>();
    // FI
    public DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    public DbSet<APEntryEntity> APEntries => Set<APEntryEntity>();
    public DbSet<AREntryEntity> AREntries => Set<AREntryEntity>();
    public DbSet<FixedAssetEntity> FixedAssets => Set<FixedAssetEntity>();
    // CO - Controlling
    public DbSet<CostCenterEntity> CostCenters => Set<CostCenterEntity>();
    public DbSet<CostElementEntity> CostElements => Set<CostElementEntity>();
    public DbSet<ProfitCenterEntity> ProfitCenters => Set<ProfitCenterEntity>();
    public DbSet<InternalOrderEntity> InternalOrders => Set<InternalOrderEntity>();
    // PS - Project System
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<ProjectTaskEntity> ProjectTasks => Set<ProjectTaskEntity>();
    public DbSet<TimesheetEntryEntity> TimesheetEntries => Set<TimesheetEntryEntity>();
    // PM - Plant Maintenance
    public DbSet<EquipmentEntity> Equipments => Set<EquipmentEntity>();
    public DbSet<MaintenancePlanEntity> MaintenancePlans => Set<MaintenancePlanEntity>();
    public DbSet<MaintenanceOrderEntity> MaintenanceOrders => Set<MaintenanceOrderEntity>();
    // HR
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<LeaveRequestEntity> LeaveRequests => Set<LeaveRequestEntity>();
    public DbSet<PayrollEntryEntity> PayrollEntries => Set<PayrollEntryEntity>();
    public DbSet<AttendanceEntity> Attendances => Set<AttendanceEntity>();
    public DbSet<AppraisalEntity> Appraisals => Set<AppraisalEntity>();
    // CRM
    public DbSet<LeadEntity> Leads => Set<LeadEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ContactEntity> Contacts => Set<ContactEntity>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<ServiceTicketEntity> ServiceTickets => Set<ServiceTicketEntity>();
    // LIMS
    public DbSet<SampleEntity> Samples => Set<SampleEntity>();
    public DbSet<TestResultEntity> TestResults => Set<TestResultEntity>();
    public DbSet<SpecificationEntity> Specifications => Set<SpecificationEntity>();
    public DbSet<InstrumentEntity> Instruments => Set<InstrumentEntity>();
    // BI
    public DbSet<BIReportEntity> BIReports => Set<BIReportEntity>();
    public DbSet<DashboardEntity> Dashboards => Set<DashboardEntity>();
    public DbSet<KpiSnapshotEntity> KpiSnapshots => Set<KpiSnapshotEntity>();
    // Audit
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    // Notifications
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    // Accounting
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<GeneralLedgerEntryEntity> GeneralLedgerEntries => Set<GeneralLedgerEntryEntity>();
    // Cross-cutting
    public DbSet<ApprovalRequestEntity> ApprovalRequests => Set<ApprovalRequestEntity>();
    public DbSet<CustomFieldEntity> CustomFields => Set<CustomFieldEntity>();
    public DbSet<AdminUserEntity> AdminUsers => Set<AdminUserEntity>();
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<TenantSettingEntity> TenantSettings => Set<TenantSettingEntity>();
    public DbSet<SystemConfigEntity> SystemConfigs => Set<SystemConfigEntity>();
    // Integration
    public DbSet<WebhookEntity> Webhooks => Set<WebhookEntity>();
    public DbSet<ApiClientEntity> ApiClients => Set<ApiClientEntity>();
    public DbSet<IntegrationQueueEntity> IntegrationQueues => Set<IntegrationQueueEntity>();
    public DbSet<IntegrationDeadLetterEntity> IntegrationDeadLetters => Set<IntegrationDeadLetterEntity>();
    public DbSet<WebhookDeliveryLogEntity> WebhookDeliveryLogs => Set<WebhookDeliveryLogEntity>();
    public DbSet<IntegrationConnectionEntity> IntegrationConnections => Set<IntegrationConnectionEntity>();
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();
    public DbSet<SyncLogEntity> SyncLogs => Set<SyncLogEntity>();
    public DbSet<MappingRuleEntity> MappingRules => Set<MappingRuleEntity>();
    // Plugins
    public DbSet<PluginEntity> Plugins => Set<PluginEntity>();
    public DbSet<PluginSettingEntity> PluginSettings => Set<PluginSettingEntity>();
    public DbSet<PluginTenantPermissionEntity> PluginTenantPermissions => Set<PluginTenantPermissionEntity>();
    public DbSet<NumberRangeDefinitionEntity> NumberRangeDefinitions => Set<NumberRangeDefinitionEntity>();
    // Transaction Codes
    public DbSet<MigrationEntity> Migrations => Set<MigrationEntity>();
    public DbSet<TransactionCodeEntity> TransactionCodes => Set<TransactionCodeEntity>();
    public DbSet<TransactionPermissionEntity> TransactionPermissions => Set<TransactionPermissionEntity>();
    public DbSet<TransactionLogEntity> TransactionLogs => Set<TransactionLogEntity>();
    // T-Code Generator
    public DbSet<TCodeDefinitionEntity> TCodeDefinitions => Set<TCodeDefinitionEntity>();
    public DbSet<TCodeFieldEntity> TCodeFields => Set<TCodeFieldEntity>();
    public DbSet<TCodeDataEntity> TCodeData => Set<TCodeDataEntity>();
    // Customization
    public DbSet<CustomizationTCodeFieldEntity> CustomizationTCodeFields => Set<CustomizationTCodeFieldEntity>();
    public DbSet<CustomizationTCodeLayoutEntity> CustomizationTCodeLayouts => Set<CustomizationTCodeLayoutEntity>();
    // MRP Extensions
    public DbSet<MrpRunHistoryEntity> MrpRunHistories => Set<MrpRunHistoryEntity>();
    public DbSet<MrpExceptionMessageEntity> MrpExceptionMessages => Set<MrpExceptionMessageEntity>();
    public DbSet<PlantEntity> Plants => Set<PlantEntity>();
    public DbSet<VendorLeadTimeEntity> VendorLeadTimes => Set<VendorLeadTimeEntity>();
    public DbSet<MrpCapacityLevelEntity> MrpCapacityLevels => Set<MrpCapacityLevelEntity>();
    // Workflow
    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions => Set<WorkflowDefinitionEntity>();
    public DbSet<WorkflowNodeEntity> WorkflowNodes => Set<WorkflowNodeEntity>();
    public DbSet<WorkflowEdgeEntity> WorkflowEdges => Set<WorkflowEdgeEntity>();
    public DbSet<WorkflowInstanceEntity> WorkflowInstances => Set<WorkflowInstanceEntity>();
    public DbSet<WorkflowHistoryEntity> WorkflowHistories => Set<WorkflowHistoryEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("yuktira_core");
        ConfigureEntities(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YuktiraDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureEntities(ModelBuilder mb)
    {
        foreach (var type in typeof(EntityBase).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.BaseType == typeof(EntityBase)))
        {
            var entity = mb.Entity(type);
            entity.HasKey(nameof(EntityBase.Id));
            entity.Property(nameof(EntityBase.CreatedAt)).HasDefaultValueSql("CURRENT_TIMESTAMP");
            if (type.Name.EndsWith("MasterEntity") || type.Name.EndsWith("Entity"))
                entity.ToTable(type.Name.Replace("Entity", "s"));
        }

        mb.Entity<GeneralLedgerEntryEntity>().Property(g => g.Debit).HasColumnType("decimal(18,2)");
        mb.Entity<GeneralLedgerEntryEntity>().Property(g => g.Credit).HasColumnType("decimal(18,2)");
        mb.Entity<AccountEntity>().Property(a => a.Balance).HasColumnType("decimal(18,2)");
    }

    public override int SaveChanges() { ApplyAudit(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { ApplyAudit(); return base.SaveChangesAsync(ct); }

    private void ApplyAudit()
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Modified) { entry.Entity.UpdatedAt = DateTime.UtcNow; entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false; }
        }
    }
}
