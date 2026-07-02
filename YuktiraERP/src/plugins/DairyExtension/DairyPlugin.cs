using YuktiraERP.PluginSdk;

namespace YuktiraERP.Plugins.DairyExtension;

public class DairyPlugin : IYuktiraPlugin, IPluginStartupHook, IPluginMenuHook
{
    public string Id => "yuktira.plugin.dairy";
    public string Name => "Dairy Extension";
    public string Code => "DAIRY_EXT";
    public string Version => "1.0.0";
    public string Description => "Dairy processing module with milk collection, quality testing, and procurement";
    public string? IconClass => "bi bi-cup-straw";
    public IEnumerable<string> Dependencies => new[] { "MM", "QM" };
    public PluginLifecycle Lifecycle => PluginLifecycle.Enabled;

    public Task OnStartupAsync(PluginContext context)
    {
        Console.WriteLine($"[DairyPlugin] Initializing for tenant {context.TenantId}");
        return Task.CompletedTask;
    }

    public List<MenuItem> GetMenuItems(PluginContext context)
    {
        return new List<MenuItem>
        {
            new() { Label = "Milk Collection", Url = "/dairy/collection", Icon = "bi-droplet", Order = 1 },
            new() { Label = "Fat/SNF Testing", Url = "/dairy/testing", Icon = "bi bi-flask", Order = 2 },
            new() { Label = "Procurement", Url = "/dairy/procurement", Icon = "bi-truck", Order = 3 },
            new() { Label = "Payout Calculation", Url = "/dairy/payout", Icon = "bi-calculator", Order = 4 }
        };
    }
}
