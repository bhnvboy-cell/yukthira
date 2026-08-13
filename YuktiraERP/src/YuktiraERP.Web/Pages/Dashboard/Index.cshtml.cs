using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IModuleCatalog _catalog;

    public IndexModel(IModuleCatalog catalog) => _catalog = catalog;

    public List<ModuleTile> Modules { get; set; } = new();
    public List<QuickAction> QuickActions { get; set; } = new();

    public void OnGet()
    {
        Modules = _catalog.Modules.Select(m => new ModuleTile
        {
            Code = m.Code,
            Name = m.Name,
            Icon = m.Icon,
            Color = m.Color,
            Category = m.Category,
            Url = m.BaseRoute,
        }).ToList();

        QuickActions = new List<QuickAction>
        {
            new() { Name = "Create PO",      Icon = "bi-cart-plus",    Color = "#2563eb", Url = "/MM/PO/Create" },
            new() { Name = "Create SO",      Icon = "bi-file-earmark", Color = "#059669", Url = "/SD/SalesOrder/Create" },
            new() { Name = "Create Sample",  Icon = "bi-flask",        Color = "#7c3aed", Url = "/LIMS/Sample/Create" },
            new() { Name = "Create Prod Order", Icon = "bi-gear",      Color = "#d97706", Url = "/PP/ProductionOrder/Create" },
            new() { Name = "Create GRN",     Icon = "bi-box-seam",     Color = "#0891b2", Url = "/MM/GRN/Create" },
            new() { Name = "QC Result",      Icon = "bi-clipboard-data", Color = "#dc2626", Url = "/QM/InspectionResult/Create" },
        };
    }

    public string CategoryColor(string cat) => _catalog.CategoryColor(cat);

    public string CategoryIcon(string cat) => _catalog.CategoryIcon(cat);
}

public class ModuleTile
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "#2563eb";
    public string Category { get; set; } = "";
    public string Url { get; set; } = "#";
}

public class QuickAction
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "#2563eb";
    public string Url { get; set; } = "#";
}
