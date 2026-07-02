using YuktiraERP.PluginSdk;

namespace YuktiraERP.Plugins.AdvancedQC;

public class AdvancedQCPlugin : IYuktiraPlugin, IPluginStartupHook, IPluginDocumentHook, IPluginMenuHook
{
    public string Id => "yuktira.plugin.advqc";
    public string Name => "Advanced QC";
    public string Code => "ADV_QC";
    public string Version => "1.0.0";
    public string Description => "Enhanced quality control with statistical process control (SPC), control charts, and automated COA generation";
    public string? IconClass => "bi bi-clipboard-check";
    public IEnumerable<string> Dependencies => new[] { "QM" };
    public PluginLifecycle Lifecycle => PluginLifecycle.Enabled;

    public Task OnStartupAsync(PluginContext context)
    {
        Console.WriteLine($"[AdvancedQC] SPC monitoring initialized for tenant {context.TenantId}");
        return Task.CompletedTask;
    }

    public async Task OnDocumentCreateAsync(PluginContext context, string entityType, Dictionary<string, object> document)
    {
        if (entityType == "INSPECTION_LOT")
        {
            Console.WriteLine($"[AdvancedQC] Auto-creating SPC chart for lot {document.GetValueOrDefault("lot_number")}");
            await Task.Delay(10);
        }
    }

    public List<MenuItem> GetMenuItems(PluginContext context)
    {
        return new List<MenuItem>
        {
            new() { Label = "SPC Dashboard", Url = "/qc/spc", Icon = "bi-graph-up", Order = 1 },
            new() { Label = "Control Charts", Url = "/qc/control-charts", Icon = "bi-bar-chart", Order = 2 },
            new() { Label = "Auto COA", Url = "/qc/auto-coa", Icon = "bi-file-text", Order = 3 },
            new() { Label = "QC Analytics", Url = "/qc/analytics", Icon = "bi-pie-chart", Order = 4 }
        };
    }
}
