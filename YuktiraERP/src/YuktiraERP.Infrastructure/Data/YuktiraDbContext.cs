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
    public DbSet<ProductionOrderItemEntity> ProductionOrderItems => Set<ProductionOrderItemEntity>();
    public DbSet<MaterialStagingEntity> MaterialStagings => Set<MaterialStagingEntity>();
    public DbSet<OrderConfirmationEntity> OrderConfirmations => Set<OrderConfirmationEntity>();
    // QM
    public DbSet<InspectionLotEntity> InspectionLots => Set<InspectionLotEntity>();
    public DbSet<InspectionPlanEntity> InspectionPlans => Set<InspectionPlanEntity>();
    public DbSet<InspectionResultEntity> InspectionResults => Set<InspectionResultEntity>();
    public DbSet<UsageDecisionEntity> UsageDecisions => Set<UsageDecisionEntity>();
    public DbSet<QualityNotificationEntity> QualityNotifications => Set<QualityNotificationEntity>();
    public DbSet<QualityNotificationTaskEntity> QualityNotificationTasks => Set<QualityNotificationTaskEntity>();
    public DbSet<InspectionResultDetailEntity> InspectionResultDetails => Set<InspectionResultDetailEntity>();
    public DbSet<UsageDecisionDetailEntity> UsageDecisionDetails => Set<UsageDecisionDetailEntity>();
    public DbSet<QMMasterDataEntity> QMMasterDatas => Set<QMMasterDataEntity>();
    public DbSet<QMInspectionConfigEntity> QMInspectionConfigs => Set<QMInspectionConfigEntity>();
    public DbSet<CertificateOfAnalysisEntity> CertificatesOfAnalysis => Set<CertificateOfAnalysisEntity>();
    // WM
    public DbSet<WarehouseTransferEntity> WarehouseTransfers => Set<WarehouseTransferEntity>();
    public DbSet<StorageLocationEntity> StorageLocations => Set<StorageLocationEntity>();
    public DbSet<BinEntity> Bins => Set<BinEntity>();
    public DbSet<TransferOrderEntity> TransferOrders => Set<TransferOrderEntity>();
    public DbSet<WaveEntity> Waves => Set<WaveEntity>();
    public DbSet<InventoryCountEntity> InventoryCounts => Set<InventoryCountEntity>();
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
    public DbSet<FunctionalLocationEntity> FunctionalLocations => Set<FunctionalLocationEntity>();
    public DbSet<MaintenanceNotificationEntity> MaintenanceNotifications => Set<MaintenanceNotificationEntity>();
    public DbSet<MaintenancePlanEntity> MaintenancePlans => Set<MaintenancePlanEntity>();
    public DbSet<MaintenanceOrderEntity> MaintenanceOrders => Set<MaintenanceOrderEntity>();
    public DbSet<SparePartEntity> SpareParts => Set<SparePartEntity>();
    // HR
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<LeaveRequestEntity> LeaveRequests => Set<LeaveRequestEntity>();
    public DbSet<PayrollEntryEntity> PayrollEntries => Set<PayrollEntryEntity>();
    public DbSet<AttendanceEntity> Attendances => Set<AttendanceEntity>();
    public DbSet<AppraisalEntity> Appraisals => Set<AppraisalEntity>();
    public DbSet<OrgUnitEntity> OrgUnits => Set<OrgUnitEntity>();
    public DbSet<TimeEntryEntity> TimeEntries => Set<TimeEntryEntity>();
    public DbSet<RecruitmentEntity> Recruitments => Set<RecruitmentEntity>();
    // CRM
    public DbSet<LeadEntity> Leads => Set<LeadEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ContactEntity> Contacts => Set<ContactEntity>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<ServiceTicketEntity> ServiceTickets => Set<ServiceTicketEntity>();
    public DbSet<CrmAccountEntity> CrmAccounts => Set<CrmAccountEntity>();
    public DbSet<SalesPipelineEntity> SalesPipelines => Set<SalesPipelineEntity>();
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
    public DbSet<TaxCodeEntity> TaxCodes => Set<TaxCodeEntity>();
    public DbSet<TaxTransactionEntity> TaxTransactions => Set<TaxTransactionEntity>();
    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
    public DbSet<MessageDeliveryEntity> MessageDeliveries => Set<MessageDeliveryEntity>();
    public DbSet<CostAllocationRuleEntity> CostAllocationRules => Set<CostAllocationRuleEntity>();
    public DbSet<CostAllocationRunEntity> CostAllocationRuns => Set<CostAllocationRunEntity>();
    public DbSet<CostAllocationDetailEntity> CostAllocationDetails => Set<CostAllocationDetailEntity>();
    public DbSet<LanguageEntity> Languages => Set<LanguageEntity>();
    public DbSet<TranslationEntity> Translations => Set<TranslationEntity>();
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
    public DbSet<EdiTradingPartnerEntity> EdiTradingPartners => Set<EdiTradingPartnerEntity>();
    public DbSet<EdiAcknowledgmentEntity> EdiAcknowledgmentLogs => Set<EdiAcknowledgmentEntity>();
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
    // Stock movements + finance loop
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    // Batch & Serial Lifecycle Management
    public DbSet<BatchEntity> Batches => Set<BatchEntity>();
    public DbSet<SerialNumberEntity> SerialNumbers => Set<SerialNumberEntity>();
    public DbSet<BatchMovementEntity> BatchMovements => Set<BatchMovementEntity>();
    public DbSet<RecallEntity> Recalls => Set<RecallEntity>();
    public DbSet<FiscalPeriodEntity> FiscalPeriods => Set<FiscalPeriodEntity>();
    public DbSet<BankReconciliationEntity> BankReconciliations => Set<BankReconciliationEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<DepreciationScheduleEntity> DepreciationSchedules => Set<DepreciationScheduleEntity>();
    public DbSet<ApprovalStepEntity> ApprovalSteps => Set<ApprovalStepEntity>();
    // Movement Type Registry (MIGO)
    public DbSet<MovementTypeEntity> MovementTypes => Set<MovementTypeEntity>();
    public DbSet<MovementTypeCategoryEntity> MovementTypeCategories => Set<MovementTypeCategoryEntity>();
    public DbSet<MovementTypeStockTypeEntity> MovementTypeStockTypes => Set<MovementTypeStockTypeEntity>();
    public DbSet<MovementTypePostingRuleEntity> MovementTypePostingRules => Set<MovementTypePostingRuleEntity>();
    public DbSet<MovementTypeIntegrationEntity> MovementTypeIntegrations => Set<MovementTypeIntegrationEntity>();
    public DbSet<MovementDocumentEntity> MovementDocuments => Set<MovementDocumentEntity>();
    public DbSet<MovementDocumentLineEntity> MovementDocumentLines => Set<MovementDocumentLineEntity>();
    public DbSet<MovementTypeWorkflowEntity> MovementTypeWorkflows => Set<MovementTypeWorkflowEntity>();
    // Procure-to-Pay
    public DbSet<PurchaseRequisitionItemEntity> PurchaseRequisitionItems => Set<PurchaseRequisitionItemEntity>();
    public DbSet<PurchaseOrderItemEntity> PurchaseOrderItems => Set<PurchaseOrderItemEntity>();
    public DbSet<DepartmentKeyEntity> DepartmentKeys => Set<DepartmentKeyEntity>();
    public DbSet<ReleaseStrategyEntity> ReleaseStrategies => Set<ReleaseStrategyEntity>();
    public DbSet<ReleaseCodeEntity> ReleaseCodes => Set<ReleaseCodeEntity>();
    // Customer Complaint & Return with Supplier Pass-Through Claim
    public DbSet<CustomerComplaintReturnEntity> CustomerComplaintReturns => Set<CustomerComplaintReturnEntity>();
    public DbSet<ReturnDeliveryEntity> ReturnDeliveries => Set<ReturnDeliveryEntity>();
    public DbSet<QualityInspectionReturnEntity> QualityInspectionReturns => Set<QualityInspectionReturnEntity>();
    public DbSet<ComplaintFinancialPostingEntity> ComplaintFinancialPostings => Set<ComplaintFinancialPostingEntity>();
    public DbSet<SupplierClaimEntity> SupplierClaims => Set<SupplierClaimEntity>();
    public DbSet<SupplierReturnDeliveryEntity> SupplierReturnDeliveries => Set<SupplierReturnDeliveryEntity>();
    public DbSet<ComplaintWorkflowStepEntity> ComplaintWorkflowSteps => Set<ComplaintWorkflowStepEntity>();
    // Phase 1.2: SOX Compliance
    public DbSet<SoxDutyEntity> SoxDuties => Set<SoxDutyEntity>();
    public DbSet<SoxAssignmentEntity> SoxAssignments => Set<SoxAssignmentEntity>();
    public DbSet<SoxViolationEntity> SoxViolations => Set<SoxViolationEntity>();
    public DbSet<ImmutableAuditTrailEntity> ImmutableAuditTrails => Set<ImmutableAuditTrailEntity>();
    // Phase 1.1: Universal Journal
    public DbSet<UniversalJournalEntity> UniversalJournals => Set<UniversalJournalEntity>();
    // Phase 1.3: RF Framework
    public DbSet<RFSessionEntity> RFSessions => Set<RFSessionEntity>();
    public DbSet<RFTransactionEntity> RFTransactions => Set<RFTransactionEntity>();
    public DbSet<RFMenuItemEntity> RFMenuItems => Set<RFMenuItemEntity>();
    public DbSet<RFPickTaskEntity> RFPickTasks => Set<RFPickTaskEntity>();
    public DbSet<RFCountTaskEntity> RFCountTasks => Set<RFCountTaskEntity>();
    // Phase 2.1: Wave Pick & Slotting
    public DbSet<WavePickEntity> WavePicks => Set<WavePickEntity>();
    public DbSet<WavePickLineEntity> WavePickLines => Set<WavePickLineEntity>();
    public DbSet<VelocitySlottingEntity> VelocitySlottings => Set<VelocitySlottingEntity>();
    public DbSet<BinMasterEntity> BinMasters => Set<BinMasterEntity>();
    // Phase 2.2: PP/DS Scheduling
    public DbSet<FiniteScheduleEntity> FiniteSchedules => Set<FiniteScheduleEntity>();
    public DbSet<FiniteScheduleOperationEntity> FiniteScheduleOperations => Set<FiniteScheduleOperationEntity>();
    public DbSet<CapacityLoadEntity> CapacityLoads => Set<CapacityLoadEntity>();
    public DbSet<MaterialAvailabilityEntity> MaterialAvailabilities => Set<MaterialAvailabilityEntity>();
    // Phase 2.3: MRP Events
    public DbSet<MrpEventEntity> MrpEvents => Set<MrpEventEntity>();
    public DbSet<MrpEventStreamEntity> MrpEventStreams => Set<MrpEventStreamEntity>();
    public DbSet<MrpPlanningRunEntity> MrpPlanningRuns => Set<MrpPlanningRunEntity>();
    public DbSet<MrpEventSubscriptionEntity> MrpEventSubscriptions => Set<MrpEventSubscriptionEntity>();
    // Phase 3.1: Consolidation
    public DbSet<ConsolidationGroupEntity> ConsolidationGroups => Set<ConsolidationGroupEntity>();
    public DbSet<ConsolidationEntityEntity> ConsolidationEntities => Set<ConsolidationEntityEntity>();
    public DbSet<InterCompanyTransactionEntity> InterCompanyTransactions => Set<InterCompanyTransactionEntity>();
    public DbSet<ConsolidationEliminationEntity> ConsolidationEliminations => Set<ConsolidationEliminationEntity>();
    public DbSet<CurrencyTranslationEntity> CurrencyTranslations => Set<CurrencyTranslationEntity>();
    // Phase 3.2: Localization Tax
    public DbSet<LocalizationCountryEntity> LocalizationCountries => Set<LocalizationCountryEntity>();
    public DbSet<LocalizationTaxConfigEntity> LocalizationTaxConfigs => Set<LocalizationTaxConfigEntity>();
    public DbSet<TaxReturnEntity> TaxReturns => Set<TaxReturnEntity>();
    public DbSet<WithholdingTaxEntity> WithholdingTaxes => Set<WithholdingTaxEntity>();
    // Phase 3.3: AI Gateway
    public DbSet<AiDocumentOcrEntity> AiDocumentOcrs => Set<AiDocumentOcrEntity>();
    public DbSet<AiDocumentTemplateEntity> AiDocumentTemplates => Set<AiDocumentTemplateEntity>();
    public DbSet<AiPredictiveModelEntity> AiPredictiveModels => Set<AiPredictiveModelEntity>();
    public DbSet<AiForecastEntity> AiForecasts => Set<AiForecastEntity>();
    public DbSet<AiAnomalyEntity> AiAnomalies => Set<AiAnomalyEntity>();
    // ATP/CTP - Stock Reservations and Allocations
    public DbSet<StockReservationEntity> StockReservations => Set<StockReservationEntity>();
    public DbSet<StockAllocationEntity> StockAllocations => Set<StockAllocationEntity>();
    // Bank Statement Import
    public DbSet<BankStatementEntity> BankStatements => Set<BankStatementEntity>();
    public DbSet<BankStatementLineEntity> BankStatementLines => Set<BankStatementLineEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        }
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
