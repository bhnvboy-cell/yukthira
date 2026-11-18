using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Core.Services;
using YuktiraERP.Mes.EdgeGateway;
using YuktiraERP.Mes.Infrastructure.Data;
using YuktiraERP.Mes.Infrastructure.Services;
using YuktiraERP.Mes.Plugin;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Mes.Tests;

public class MesPluginTests
{
    private static MesDbContext CreateInMemoryDb()
    {
        var opts = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase($"MesTest_{Guid.NewGuid()}")
            .Options;
        return new MesDbContext(opts);
    }

    [Fact]
    public void PluginMetadata_And_Hooks_AreValid()
    {
        var plugin = new YuktiraMesPlugin();
        Assert.Equal("MES", plugin.Code);
        Assert.Equal(PluginLifecycle.Enabled, plugin.Lifecycle);
        Assert.Contains("PP", plugin.Dependencies);

        var ctx = new PluginContext { TenantId = Guid.NewGuid() };
        var menu = plugin.GetMenuItems(ctx);
        Assert.Single(menu);
        Assert.Equal(7, menu[0].Children!.Count);

        var perms = plugin.GetPermissions();
        Assert.Equal(7, perms.Count);
        Assert.Contains(perms, p => p.Code == "MES01");
        Assert.Contains(perms, p => p.Code == "MES07");

        var settings = plugin.GetSettingDefinitions();
        Assert.Contains(settings, s => s.Key == "NegativeStockPolicy");
    }

    [Fact]
    public void OeeCalculator_ComputesCorrectly_And_ExcludesDataGaps()
    {
        var tenantId = Guid.NewGuid();
        var resId = Guid.NewGuid();
        var interval = OeeCalculator.ComputeInterval(
            tenantId, "PLANT01", resId, DateTime.UtcNow, OeeIntervalType.SHIFT,
            plannedTimeS: 1000, runTimeS: 900, idealCycleS: 1.0,
            totalCount: 800, goodCount: 760, isDataGapped: false);

        Assert.False(interval.IsDataGapped);
        Assert.Equal(0.9, interval.Availability);
        Assert.Equal(0.8889, interval.Performance);
        Assert.Equal(0.95, interval.Quality);
        Assert.Equal(0.76, interval.Oee);

        var gapped = OeeCalculator.ComputeInterval(
            tenantId, "PLANT01", resId, DateTime.UtcNow, OeeIntervalType.MINUTE,
            plannedTimeS: 60, runTimeS: 0, idealCycleS: 1.0,
            totalCount: 0, goodCount: 0, isDataGapped: true);
        Assert.True(gapped.IsDataGapped);
        Assert.Equal(0, gapped.Oee);
    }

    [Fact]
    public void SpcRuleEngine_DetectsWesternElectricRules_And_Capability()
    {
        // CL=10, UCL=13, LCL=7 => 1 sigma = 1
        var seriesRule1 = new double[] { 10.1, 10.2, 13.5 };
        var v1 = SpcRuleEngine.EvaluateWesternElectric(seriesRule1, cl: 10, ucl: 13, lcl: 7);
        Assert.Contains("WE_RULE_1", v1);

        var seriesRule4 = new double[] { 10.2, 10.3, 10.1, 10.4, 10.2, 10.5, 10.1, 10.3 };
        var v4 = SpcRuleEngine.EvaluateWesternElectric(seriesRule4, cl: 10, ucl: 13, lcl: 7);
        Assert.Contains("WE_RULE_4", v4);

        var (cp, cpk) = SpcRuleEngine.ComputeCapability(new double[] { 10.0, 10.1, 9.9, 10.0, 10.05 }, 9.0, 11.0);
        Assert.True(cp > 1.33);
        Assert.True(cpk > 1.33);
    }

    [Fact]
    public async Task DispatchService_ConfirmsIdempotently_WritesOutbox_And_PromotesNextOp()
    {
        await using var db = CreateInMemoryDb();
        var svc = new MesDispatchService(db);
        var tenantId = Guid.NewGuid();
        var wcId = Guid.NewGuid();
        var resId = Guid.NewGuid();

        var wo = await svc.CreateWorkOrderAsync(tenantId, "PLANT01", "WO-1001", Guid.NewGuid(), Guid.NewGuid(), 10, DateTime.UtcNow.AddDays(2));
        var op10 = await svc.AddOperationAsync(tenantId, "PLANT01", wo.Id, 10, wcId, resId, 10, 5, 20, ready: true);
        var op20 = await svc.AddOperationAsync(tenantId, "PLANT01", wo.Id, 20, wcId, resId, 10, 5, 30, ready: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.StartOperationAsync(tenantId, op20.Id, "op1"));

        await svc.StartOperationAsync(tenantId, op10.Id, "op1");
        var idemp = Guid.NewGuid();
        var c1 = await svc.ConfirmOperationAsync(tenantId, op10.Id, idemp, 10, 0, 0, null, 15, 20, "op1");
        var c2 = await svc.ConfirmOperationAsync(tenantId, op10.Id, idemp, 10, 0, 0, null, 15, 20, "op1");

        Assert.Equal(c1.Id, c2.Id);
        Assert.Single(db.OutboxEvents);
        Assert.Single(db.ErpPostingLogs);

        var updatedOp20 = await db.WoOperations.FirstAsync(o => o.Id == op20.Id);
        Assert.Equal(WoOperationStatus.READY, updatedOp20.Status);
    }

    [Fact]
    public async Task DataCollectionService_DeduplicatesTags_And_SuppressesDuplicateScans()
    {
        await using var db = CreateInMemoryDb();
        var svc = new MesDataCollectionService(db);
        var tenantId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var msgId = Guid.NewGuid();

        var sample = new MesTagValueRaw { TagId = tagId, ValueNum = 42.5, Quality = TagQuality.GOOD, Seq = 1, MessageUuid = msgId };
        var (ins1, dedup1) = await svc.IngestTagBatchAsync(tenantId, "PLANT01", "EDGE-01", new[] { sample });
        var (ins2, dedup2) = await svc.IngestTagBatchAsync(tenantId, "PLANT01", "EDGE-01", new[] { sample });

        Assert.Equal(1, ins1);
        Assert.Equal(0, dedup1);
        Assert.Equal(0, ins2);
        Assert.Equal(1, dedup2);

        var s1 = await svc.RecordScanAsync(tenantId, "PLANT01", "TERM-1", null, "LOT-999", "BARCODE", Guid.NewGuid(), 2);
        var s2 = await svc.RecordScanAsync(tenantId, "PLANT01", "TERM-1", null, "LOT-999", "BARCODE", Guid.NewGuid(), 2);
        Assert.Equal(ScanResult.ACCEPTED, s1.Result);
        Assert.Equal(ScanResult.DUPLICATE, s2.Result);
    }

    [Fact]
    public async Task QualityAndGenealogy_AutoHoldOnOot_And_TraceForwardBackward()
    {
        await using var db = CreateInMemoryDb();
        var qm = new MesQualityService(db);
        var gen = new MesGenealogyService(db);
        var tenantId = Guid.NewGuid();

        var vendorLotId = Guid.NewGuid();
        var subAssemblyId = Guid.NewGuid();
        var finishedUnitId = Guid.NewGuid();

        await gen.RecordEdgeAsync(tenantId, "PLANT01", "LOT", vendorLotId, "UNIT", subAssemblyId, 1, GenealogyEdgeType.CONSUMED_IN);
        await gen.RecordEdgeAsync(tenantId, "PLANT01", "UNIT", subAssemblyId, "UNIT", finishedUnitId, 1, GenealogyEdgeType.CONSUMED_IN);

        var forward = await gen.TraceForwardDescendantsAsync(tenantId, vendorLotId);
        Assert.Contains(subAssemblyId, forward);
        Assert.Contains(finishedUnitId, forward);

        var backward = await gen.TraceBackwardAncestorsAsync(tenantId, finishedUnitId);
        Assert.Contains(vendorLotId, backward);
        Assert.Contains(subAssemblyId, backward);

        var oot = await qm.RecordQcResultAsync(tenantId, "PLANT01", Guid.NewGuid(), Guid.NewGuid(), 1, 15.5, "mm", 9.5, 10.5, finishedUnitId, vendorLotId);
        Assert.False(oot.InTolerance);
        Assert.Single(db.NonConformances);
        var hold = await db.Holds.FirstAsync();
        Assert.Equal(HoldStatus.ACTIVE, hold.Status);
        Assert.False(string.IsNullOrEmpty(hold.RecordHash));
    }

    [Fact]
    public void EdgeGateway_DeadbandAndBuffer_WorkAsExpected()
    {
        var filter = new DeadbandFilter();
        var tagId = Guid.NewGuid();
        Assert.True(filter.ShouldEmit(tagId, 100.0, 0.5));
        Assert.False(filter.ShouldEmit(tagId, 100.2, 0.5));
        Assert.True(filter.ShouldEmit(tagId, 100.6, 0.5));

        var buf = new EdgeStoreAndForwardBuffer();
        buf.Enqueue(new EdgeTagReading(tagId, DateTime.UtcNow, 100.6, null, TagQuality.GOOD, 1, Guid.NewGuid()));
        Assert.Equal(1, buf.Depth);
        var batch = buf.DequeueBatch(10);
        Assert.Single(batch);
        Assert.Equal(0, buf.Depth);
    }

    [Fact]
    public async Task Scheduling_Document_Labor_Process_Maintenance_EndToEnd()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var resId = Guid.NewGuid();

        // Scheduling (Function 2)
        var dispatch = new MesDispatchService(db);
        var wo = await dispatch.CreateWorkOrderAsync(tenantId, "PLANT01", "WO-2001", Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow.AddDays(1));
        await dispatch.AddOperationAsync(tenantId, "PLANT01", wo.Id, 10, Guid.NewGuid(), resId, 5, 10, 5, ready: true);

        var schedSvc = new MesSchedulingService(db);
        var ver = await schedSvc.CreateScheduleVersionAsync(tenantId, "PLANT01", "V1", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        var ops = await schedSvc.SequenceOperationsAsync(tenantId, ver.Id, resId, new[] { wo.Id });
        Assert.Single(ops);
        await schedSvc.PublishAsync(tenantId, ver.Id);

        // Document Control & EBR (Function 4)
        var docSvc = new MesDocumentService(db);
        var (_, rev) = await docSvc.CreateDocumentRevisionAsync(tenantId, "PLANT01", "SOP-001", MesDocType.SOP, "Mixing SOP", Guid.NewGuid(), "A", "abc123", "s3://docs/sop1.pdf");
        await docSvc.MakeEffectiveAsync(tenantId, rev.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => docSvc.RetrieveVerifiedRevisionAsync(tenantId, rev.Id, "tampered_hash"));
        var sig = await docSvc.SignAsync(tenantId, "PLANT01", Guid.NewGuid(), ESignatureMeaning.APPROVED);
        var ebrEntry = await docSvc.RecordEbrStepAsync(tenantId, "PLANT01", Guid.NewGuid(), 1, "120", "C", 90m, 110m, "op1", sig.Id);
        Assert.True(ebrEntry.DeviationFlag);

        // Labor Management (Function 6)
        var laborSvc = new MesLaborService(db);
        var empId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        db.OperationSkillReqs.Add(new MesOperationSkillReq { TenantId = tenantId, ResourceId = resId, SkillId = skillId, MinLevel = 3 });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => laborSvc.StartTaskAsync(tenantId, "PLANT01", empId, null, resId, LaborActivityType.DIRECT));

        // Process & Maintenance (Functions 8 & 9)
        var procSvc = new MesProcessService(db);
        var interlock = new MesInterlock { TenantId = tenantId, ResourceId = resId, Condition = "TEMP > 120" };
        db.Interlocks.Add(interlock);
        await db.SaveChangesAsync();
        var bypassed = await procSvc.BypassInterlockAsync(tenantId, interlock.Id, "sup1", TimeSpan.FromMinutes(15), sig.Id);
        Assert.Equal("BYPASSED", bypassed.Status);

        var maintSvc = new MesMaintenanceService(db);
        var eqId = Guid.NewGuid();
        db.PmTriggers.Add(new MesPmTrigger { TenantId = tenantId, EquipmentId = eqId, TriggerType = PmTriggerType.METER, Threshold = 100 });
        await db.SaveChangesAsync();
        var (_, pmDue) = await maintSvc.UpdateMeterAsync(tenantId, eqId, MeterType.HOURS, 105);
        Assert.True(pmDue);
        await maintSvc.IssueLotoPermitAsync(tenantId, "PLANT01", eqId, "[\"LOCK-1\"]", "tech1");
        Assert.True(await maintSvc.HasActiveLotoPermitAsync(tenantId, eqId));
    }

    [Fact]
    public void PluginLoader_LoadsYuktiraMesPlugin_FromAssembly()
    {
        var loader = new PluginLoader(Path.Combine(Path.GetTempPath(), $"mes_plug_test_{Guid.NewGuid()}"));
        var plugin = loader.LoadPlugin(typeof(YuktiraMesPlugin).Assembly.Location);
        Assert.NotNull(plugin);
        Assert.Equal("MES", plugin!.Code);
        Assert.NotNull(loader.GetPluginHook<IPluginWebModule>());
    }

    [Fact]
    public void PluginWebModule_RegistersAllMesServicesAndControllers()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        var plugin = new YuktiraMesPlugin();
        plugin.ConfigureServices(services, config);

        using var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(sp);
        var p = scope.ServiceProvider;

        Assert.NotNull(p.GetService(typeof(MesDbContext)));
        Assert.NotNull(p.GetService(typeof(MesResourceService)));
        Assert.NotNull(p.GetService(typeof(MesSchedulingService)));
        Assert.NotNull(p.GetService(typeof(MesDispatchService)));
        Assert.NotNull(p.GetService(typeof(MesDocumentService)));
        Assert.NotNull(p.GetService(typeof(MesDataCollectionService)));
        Assert.NotNull(p.GetService(typeof(MesLaborService)));
        Assert.NotNull(p.GetService(typeof(MesQualityService)));
        Assert.NotNull(p.GetService(typeof(MesProcessService)));
        Assert.NotNull(p.GetService(typeof(MesMaintenanceService)));
        Assert.NotNull(p.GetService(typeof(MesGenealogyService)));
        Assert.NotNull(p.GetService(typeof(MesOeeService)));
        Assert.NotNull(p.GetService(typeof(MesErpPostingBridge)));

        var controllerTypes = ((IPluginWebModule)plugin).GetControllerAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        Assert.Equal(12, controllerTypes.Count);
    }
}
