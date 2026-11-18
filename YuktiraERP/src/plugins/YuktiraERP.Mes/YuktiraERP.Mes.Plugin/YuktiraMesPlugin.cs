using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Mes.Infrastructure;
using YuktiraERP.Mes.Plugin.Hubs;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Mes.Plugin;

public class YuktiraMesPlugin :
    IYuktiraPlugin,
    IPluginWebModule,
    IPluginStartupHook,
    IPluginMenuHook,
    IPluginPermissionProvider,
    IPluginConfigurable,
    IPluginDocumentHook
{
    public string Id => "yuktira.plugin.mes";
    public string Name => "Yukthira MES (MESA-11)";
    public string Code => "MES";
    public string Version => "1.0.0";
    public string Description => "Shop-Floor Execution System (MESA-11): Resource Status, Detailed Scheduling, Dispatching, Document Control/EBR, Data Collection, Labor, Quality/SPC, Process Management, Maintenance, Genealogy, and OEE.";
    public string? IconClass => "bi bi-cpu";
    public IEnumerable<string> Dependencies => new[] { "PP", "MM", "QM", "PM", "HR", "WM", "CO" };
    public PluginLifecycle Lifecycle => PluginLifecycle.Enabled;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddYuktiraMesInfrastructure(configuration);
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<MesHub>("/hubs/mes");
    }

    public Task OnStartupAsync(PluginContext context)
    {
        Console.WriteLine($"[YuktiraMES] MESA-11 Shop-Floor Execution plugin initialized for tenant {context.TenantId}");
        return Task.CompletedTask;
    }

    public Task OnDocumentCreateAsync(PluginContext context, string entityType, Dictionary<string, object> document)
    {
        if (entityType is "PRODUCTION_ORDER" or "INSPECTION_LOT")
        {
            Console.WriteLine($"[YuktiraMES] Observed {entityType} event for tenant {context.TenantId}");
        }
        return Task.CompletedTask;
    }

    public List<MenuItem> GetMenuItems(PluginContext context)
    {
        return new List<MenuItem>
        {
            new()
            {
                Label = "MES Shop Floor",
                Url = "/mes",
                Icon = "bi-cpu",
                Order = 50,
                Permission = "MES.VIEW",
                Children = new List<MenuItem>
                {
                    new() { Label = "MES01 Dispatch Board", Url = "/mes/dispatch", Icon = "bi-kanban", Order = 1, Permission = "MES01" },
                    new() { Label = "MES02 Resource Status", Url = "/mes/resources", Icon = "bi-hdd-rack", Order = 2, Permission = "MES02" },
                    new() { Label = "MES03 Genealogy Trace", Url = "/mes/genealogy", Icon = "bi-diagram-3", Order = 3, Permission = "MES03" },
                    new() { Label = "MES04 SPC & Quality", Url = "/mes/spc", Icon = "bi-graph-up-arrow", Order = 4, Permission = "MES04" },
                    new() { Label = "MES05 Process Alarms", Url = "/mes/alarms", Icon = "bi-bell", Order = 5, Permission = "MES05" },
                    new() { Label = "MES06 OEE Cockpit", Url = "/mes/oee", Icon = "bi-speedometer2", Order = 6, Permission = "MES06" },
                    new() { Label = "MES07 Exception Cockpit", Url = "/mes/exceptions", Icon = "bi-exclamation-triangle", Order = 7, Permission = "MES07" }
                }
            }
        };
    }

    public List<PluginPermission> GetPermissions()
    {
        return new List<PluginPermission>
        {
            new() { Code = "MES01", Label = "Dispatch Board", Description = "View and execute shop-floor dispatch queue and confirmations", Group = "MES" },
            new() { Code = "MES02", Label = "Resource Status", Description = "View and override machine/resource states and tool life", Group = "MES" },
            new() { Code = "MES03", Label = "Genealogy Trace", Description = "Perform forward/backward lot and serial genealogy traces", Group = "MES" },
            new() { Code = "MES04", Label = "SPC & Quality", Description = "Record QC results, evaluate SPC charts, and manage holds", Group = "MES" },
            new() { Code = "MES05", Label = "Process Alarms", Description = "Acknowledge process alarms and manage interlocks", Group = "MES" },
            new() { Code = "MES06", Label = "OEE Cockpit", Description = "View OEE, TEEP, and Six Big Losses KPIs", Group = "MES" },
            new() { Code = "MES07", Label = "Exception Cockpit", Description = "Manage failed ERP postings, dead letters, and overrides", Group = "MES" }
        };
    }

    public List<PluginSettingDefinition> GetSettingDefinitions()
    {
        return new List<PluginSettingDefinition>
        {
            new()
            {
                Key = "NegativeStockPolicy",
                Label = "Negative Stock Policy",
                Type = "select",
                DefaultValue = "ALLOW_WITH_FLAG",
                Options = new List<string> { "BLOCK", "ALLOW_WITH_FLAG", "AUTO_TRANSFER" },
                Description = "Floor policy when component backflush exceeds recorded line-side stock"
            },
            new()
            {
                Key = "DuplicateScanWindowSec",
                Label = "Duplicate Scan Window (s)",
                Type = "number",
                DefaultValue = "2",
                Description = "Ignore duplicate barcode/RFID scans within this window"
            },
            new()
            {
                Key = "UseTimescaleDb",
                Label = "Use TimescaleDB Hypertables",
                Type = "boolean",
                DefaultValue = "false",
                Description = "Use TimescaleDB hypertables for mes_tag_value_raw instead of native PostgreSQL 18 partitioning"
            },
            new()
            {
                Key = "RegulatedEbrMode",
                Label = "Regulated Industry EBR & 21 CFR Part 11",
                Type = "boolean",
                DefaultValue = "false",
                Description = "Enforce mandatory e-signatures and EBR step verification before QM usage decision"
            }
        };
    }

    public Task ApplySettingsAsync(Dictionary<string, string> settings)
    {
        return Task.CompletedTask;
    }
}
