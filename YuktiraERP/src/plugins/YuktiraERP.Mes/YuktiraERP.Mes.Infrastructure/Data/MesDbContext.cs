using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;

namespace YuktiraERP.Mes.Infrastructure.Data;

/// <summary>
/// Dedicated EF Core DbContext for the MES plugin, isolated in PostgreSQL schema 'yuktira_mes'.
/// Shares TenantSaveChangesInterceptor and tenant isolation with YuktiraERP.
/// </summary>
public class MesDbContext : DbContext
{
    public const string SchemaName = "yuktira_mes";

    public MesDbContext(DbContextOptions<MesDbContext> options) : base(options) { }

    // Function 1: Resource Allocation & Status
    public DbSet<MesResource> Resources => Set<MesResource>();
    public DbSet<MesResourceStateLog> ResourceStateLogs => Set<MesResourceStateLog>();
    public DbSet<MesStateReason> StateReasons => Set<MesStateReason>();
    public DbSet<MesResourceAllocation> ResourceAllocations => Set<MesResourceAllocation>();
    public DbSet<MesToolLife> ToolLives => Set<MesToolLife>();
    public DbSet<MesResourceCapability> ResourceCapabilities => Set<MesResourceCapability>();

    // Function 2: Operations & Detailed Scheduling
    public DbSet<MesScheduleVersion> ScheduleVersions => Set<MesScheduleVersion>();
    public DbSet<MesScheduleOp> ScheduleOps => Set<MesScheduleOp>();
    public DbSet<MesChangeoverMatrix> ChangeoverMatrices => Set<MesChangeoverMatrix>();
    public DbSet<MesShiftCalendar> ShiftCalendars => Set<MesShiftCalendar>();
    public DbSet<MesScheduleConflict> ScheduleConflicts => Set<MesScheduleConflict>();
    public DbSet<MesPriorityRule> PriorityRules => Set<MesPriorityRule>();

    // Function 3: Dispatching Production Units
    public DbSet<MesWorkOrder> WorkOrders => Set<MesWorkOrder>();
    public DbSet<MesWoOperation> WoOperations => Set<MesWoOperation>();
    public DbSet<MesDispatchQueue> DispatchQueues => Set<MesDispatchQueue>();
    public DbSet<MesDispatchEvent> DispatchEvents => Set<MesDispatchEvent>();
    public DbSet<MesOpConfirmation> OpConfirmations => Set<MesOpConfirmation>();

    // Function 4: Document Control & EBR
    public DbSet<MesDocument> Documents => Set<MesDocument>();
    public DbSet<MesDocumentRevision> DocumentRevisions => Set<MesDocumentRevision>();
    public DbSet<MesDocumentLink> DocumentLinks => Set<MesDocumentLink>();
    public DbSet<MesDocAcknowledgment> DocAcknowledgments => Set<MesDocAcknowledgment>();
    public DbSet<MesEbrInstance> EbrInstances => Set<MesEbrInstance>();
    public DbSet<MesEbrStepEntry> EbrStepEntries => Set<MesEbrStepEntry>();
    public DbSet<MesESignature> ESignatures => Set<MesESignature>();

    // Function 5: Data Collection & Acquisition
    public DbSet<MesEdgeNode> EdgeNodes => Set<MesEdgeNode>();
    public DbSet<MesDataSource> DataSources => Set<MesDataSource>();
    public DbSet<MesTag> Tags => Set<MesTag>();
    public DbSet<MesTagValueRaw> TagValuesRaw => Set<MesTagValueRaw>();
    public DbSet<MesTagValue1m> TagValues1m => Set<MesTagValue1m>();
    public DbSet<MesScanEvent> ScanEvents => Set<MesScanEvent>();
    public DbSet<MesManualEntry> ManualEntries => Set<MesManualEntry>();
    public DbSet<MesDeadLetter> DeadLetters => Set<MesDeadLetter>();

    // Function 6: Labor Management
    public DbSet<MesLaborClock> LaborClocks => Set<MesLaborClock>();
    public DbSet<MesLaborTaskLog> LaborTaskLogs => Set<MesLaborTaskLog>();
    public DbSet<MesSkill> Skills => Set<MesSkill>();
    public DbSet<MesEmployeeSkill> EmployeeSkills => Set<MesEmployeeSkill>();
    public DbSet<MesOperationSkillReq> OperationSkillReqs => Set<MesOperationSkillReq>();
    public DbSet<MesShiftAssignment> ShiftAssignments => Set<MesShiftAssignment>();
    public DbSet<MesLaborException> LaborExceptions => Set<MesLaborException>();

    // Function 7: Quality Management & SPC
    public DbSet<MesInspectionTask> InspectionTasks => Set<MesInspectionTask>();
    public DbSet<MesQcResult> QcResults => Set<MesQcResult>();
    public DbSet<MesSpcChart> SpcCharts => Set<MesSpcChart>();
    public DbSet<MesSpcPoint> SpcPoints => Set<MesSpcPoint>();
    public DbSet<MesSpcViolation> SpcViolations => Set<MesSpcViolation>();
    public DbSet<MesNonConformance> NonConformances => Set<MesNonConformance>();
    public DbSet<MesHold> Holds => Set<MesHold>();
    public DbSet<MesCapabilitySnapshot> CapabilitySnapshots => Set<MesCapabilitySnapshot>();

    // Function 8: Process Management
    public DbSet<MesRecipe> Recipes => Set<MesRecipe>();
    public DbSet<MesRecipePhase> RecipePhases => Set<MesRecipePhase>();
    public DbSet<MesRecipeParam> RecipeParams => Set<MesRecipeParam>();
    public DbSet<MesRecipeDownload> RecipeDownloads => Set<MesRecipeDownload>();
    public DbSet<MesProcessRun> ProcessRuns => Set<MesProcessRun>();
    public DbSet<MesAlarm> Alarms => Set<MesAlarm>();
    public DbSet<MesAlarmRule> AlarmRules => Set<MesAlarmRule>();
    public DbSet<MesInterlock> Interlocks => Set<MesInterlock>();

    // Function 9: Maintenance Management
    public DbSet<MesEquipmentMeter> EquipmentMeters => Set<MesEquipmentMeter>();
    public DbSet<MesPmTrigger> PmTriggers => Set<MesPmTrigger>();
    public DbSet<MesDowntimeEvent> DowntimeEvents => Set<MesDowntimeEvent>();
    public DbSet<MesAndonCall> AndonCalls => Set<MesAndonCall>();
    public DbSet<MesConditionReading> ConditionReadings => Set<MesConditionReading>();
    public DbSet<MesFailureCode> FailureCodes => Set<MesFailureCode>();
    public DbSet<MesLoto> Lotos => Set<MesLoto>();

    // Function 10: Product Tracking & Genealogy
    public DbSet<MesLot> Lots => Set<MesLot>();
    public DbSet<MesUnit> Units => Set<MesUnit>();
    public DbSet<MesContainer> Containers => Set<MesContainer>();
    public DbSet<MesGenealogyEdge> GenealogyEdges => Set<MesGenealogyEdge>();
    public DbSet<MesGenealogyClosure> GenealogyClosures => Set<MesGenealogyClosure>();
    public DbSet<MesAsBuilt> AsBuilts => Set<MesAsBuilt>();
    public DbSet<MesUnitHistory> UnitHistories => Set<MesUnitHistory>();

    // Function 11: Performance Analysis
    public DbSet<MesKpiDefinition> KpiDefinitions => Set<MesKpiDefinition>();
    public DbSet<MesOeeInterval> OeeIntervals => Set<MesOeeInterval>();
    public DbSet<MesLossEvent> LossEvents => Set<MesLossEvent>();
    public DbSet<MesCycleTimeStat> CycleTimeStats => Set<MesCycleTimeStat>();
    public DbSet<MesScheduleAdherence> ScheduleAdherences => Set<MesScheduleAdherence>();
    public DbSet<MesKpiSnapshot> KpiSnapshots => Set<MesKpiSnapshot>();

    // Cross-Cutting
    public DbSet<MesOutboxEvent> OutboxEvents => Set<MesOutboxEvent>();
    public DbSet<MesErpPostingLog> ErpPostingLogs => Set<MesErpPostingLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);

        // Store all enums as text for readability (Section 1.5)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                }
            }
        }

        // Business key uniqueness per tenant & idempotency indexes
        modelBuilder.Entity<MesResource>()
            .HasIndex(e => new { e.TenantId, e.ResourceCode }).IsUnique();

        modelBuilder.Entity<MesWorkOrder>()
            .HasIndex(e => new { e.TenantId, e.WorkOrderNumber }).IsUnique();

        modelBuilder.Entity<MesOpConfirmation>()
            .HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique();

        modelBuilder.Entity<MesEdgeNode>()
            .HasIndex(e => new { e.TenantId, e.NodeCode }).IsUnique();

        modelBuilder.Entity<MesTag>()
            .HasIndex(e => new { e.TenantId, e.TagCode }).IsUnique();

        modelBuilder.Entity<MesTagValueRaw>()
            .HasIndex(e => new { e.TenantId, e.SourceId, e.MessageUuid }).IsUnique();
        modelBuilder.Entity<MesTagValueRaw>()
            .HasIndex(e => new { e.TagId, e.Ts });

        modelBuilder.Entity<MesScanEvent>()
            .HasIndex(e => new { e.TenantId, e.SourceId, e.MessageUuid }).IsUnique();

        modelBuilder.Entity<MesLot>()
            .HasIndex(e => new { e.TenantId, e.LotNo }).IsUnique();

        modelBuilder.Entity<MesUnit>()
            .HasIndex(e => new { e.TenantId, e.SerialNo }).IsUnique();

        modelBuilder.Entity<MesGenealogyClosure>()
            .HasIndex(e => new { e.TenantId, e.AncestorId, e.DescendantId }).IsUnique();

        modelBuilder.Entity<MesDocument>()
            .HasIndex(e => new { e.TenantId, e.DocNo }).IsUnique();

        modelBuilder.Entity<MesOutboxEvent>()
            .HasIndex(e => new { e.Status, e.NextRetryAt });
    }
}
