using YuktiraERP.PluginSdk;

namespace YuktiraERP.Plugins.ExtraReports;

public class ExtraReportsPlugin : IYuktiraPlugin, IPluginMenuHook, IPluginStartupHook
{
    public string Id => "yuktira.plugin.extrareports";
    public string Name => "Extra Reports";
    public string Code => "EXTRA_REPORTS";
    public string Version => "1.0.0";
    public string Description => "Additional analytical reports: profitability analysis, variance reports, executive summaries";
    public string? IconClass => "bi bi-file-earmark-bar-graph";
    public IEnumerable<string> Dependencies => new[] { "BI", "FI" };
    public PluginLifecycle Lifecycle => PluginLifecycle.Enabled;

    public Task OnStartupAsync(PluginContext context)
    {
        Console.WriteLine($"[ExtraReports] Report templates initialized for tenant {context.TenantId}");
        return Task.CompletedTask;
    }

    public List<MenuItem> GetMenuItems(PluginContext context)
    {
        return new List<MenuItem>
        {
            new() { Label = "Profitability Analysis", Url = "/reports/profitability", Icon = "bi-currency-dollar", Order = 1 },
            new() { Label = "Variance Reports", Url = "/reports/variance", Icon = "bi-shuffle", Order = 2 },
            new() { Label = "Executive Summary", Url = "/reports/executive", Icon = "bi-file-earmark-text", Order = 3 },
            new() { Label = "Custom Report Builder", Url = "/reports/custom-builder", Icon = "bi-tools", Order = 4 }
        };
    }
}
