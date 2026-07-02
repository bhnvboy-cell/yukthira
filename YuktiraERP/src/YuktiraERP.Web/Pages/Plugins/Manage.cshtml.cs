using Microsoft.AspNetCore.Mvc.RazorPages;
namespace YuktiraERP.Web.Pages.Plugins;
public class ManageModel : PageModel
{
    public List<PluginInfo> Plugins { get; set; } = new();
    public void OnGet()
    {
        Plugins = new List<PluginInfo>
        {
            new() { Name = "Barcode Scanner Integration", Version = "1.0.0", Status = "Active" },
            new() { Name = "E-Invoicing Connector", Version = "2.1.0", Status = "Active" },
            new() { Name = "SAP Bridge", Version = "0.5.0", Status = "Inactive" },
        };
    }
}
public class PluginInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";
}
