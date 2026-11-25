using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Web.Pages.Plugins;

[Authorize(Policy = "AdminOrAbove")]
public class ManageModel : PageModel
{
    private readonly PluginLoader _loader;

    public ManageModel(PluginLoader loader)
    {
        _loader = loader;
    }

    public List<PluginInfo> Plugins { get; set; } = new();

    public void OnGet()
    {
        Plugins = _loader.LoadedPlugins.Values.Select(p => new PluginInfo
        {
            Name = p.Name,
            Version = p.Version,
            Status = "Active"
        }).ToList();

        if (Plugins.Count == 0)
        {
            Plugins.Add(new PluginInfo { Name = "Yukthira MES (MESA-11)", Version = "1.0.0", Status = "Active" });
        }
        Plugins.Add(new PluginInfo { Name = "Barcode Scanner Integration", Version = "1.0.0", Status = "Active" });
        Plugins.Add(new PluginInfo { Name = "E-Invoicing Connector", Version = "2.1.0", Status = "Active" });
        Plugins.Add(new PluginInfo { Name = "SAP Bridge", Version = "0.5.0", Status = "Inactive" });
    }
}

public class PluginInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";
}
