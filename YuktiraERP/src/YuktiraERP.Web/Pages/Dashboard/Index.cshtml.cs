using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    public List<ModuleTile> Modules { get; set; } = new();
    public List<QuickAction> QuickActions { get; set; } = new();

    public void OnGet()
    {
        Modules = new List<ModuleTile>
        {
            // ── Operations ──
            new() { Code = "MM", Name = "Materials Management",  Icon = "bi-boxes",               Color = "#2563eb", Category = "Operations",     Url = "/MM" },
            new() { Code = "SD", Name = "Sales & Distribution",  Icon = "bi-cart3",              Color = "#059669", Category = "Operations",     Url = "/SD" },
            new() { Code = "WM", Name = "Warehouse Management",  Icon = "bi-house-door",         Color = "#d97706", Category = "Operations",     Url = "/WM" },
            new() { Code = "PP", Name = "Production Planning",   Icon = "bi-gear",               Color = "#7c3aed", Category = "Operations",     Url = "/PP" },
            new() { Code = "QM", Name = "Quality Management",    Icon = "bi-clipboard-check",    Color = "#dc2626", Category = "Operations",     Url = "/QM" },
            new() { Code = "PM", Name = "Plant Maintenance",     Icon = "bi-tools",              Color = "#0891b2", Category = "Operations",     Url = "/PM" },

            // ── Finance ──
            new() { Code = "FI", Name = "Finance",               Icon = "bi-calculator",         Color = "#059669", Category = "Finance",        Url = "/FI" },
            new() { Code = "CO", Name = "Controlling",           Icon = "bi-pie-chart",          Color = "#ca8a04", Category = "Finance",        Url = "/CO" },

            // ── People ──
            new() { Code = "HR", Name = "Human Resources",       Icon = "bi-people",             Color = "#db2777", Category = "People",         Url = "/HR" },
            new() { Code = "CRM", Name = "Customer Relationship", Icon = "bi-person-lines-fill", Color = "#ea580c", Category = "People",         Url = "/CRM" },

            // ── Projects & Labs ──
            new() { Code = "PS", Name = "Project System",        Icon = "bi-diagram-3",          Color = "#4f46e5", Category = "Projects & Labs", Url = "/PS" },
            new() { Code = "LIMS", Name = "Lab Information Mgmt", Icon = "bi-flask",             Color = "#0d9488", Category = "Projects & Labs", Url = "/LIMS" },

            // ── Analytics ──
            new() { Code = "BI", Name = "BI Reports",            Icon = "bi-graph-up",           Color = "#2563eb", Category = "Analytics",      Url = "/BI" },
            new() { Code = "AI", Name = "AI Forecasting",        Icon = "bi-cpu",                Color = "#9333ea", Category = "Analytics",      Url = "/PP/Mrp?tab=forecast" },

            // ── System ──
            new() { Code = "WF", Name = "Workflows",             Icon = "bi-arrow-repeat",       Color = "#0891b2", Category = "System",         Url = "/Workflow/Designer" },
            new() { Code = "APP", Name = "Approvals",            Icon = "bi-check2-square",      Color = "#ca8a04", Category = "System",         Url = "/Approval" },
            new() { Code = "NOT", Name = "Notifications",        Icon = "bi-bell",               Color = "#db2777", Category = "System",         Url = "/Notifications" },
            new() { Code = "TCD", Name = "Transaction Codes",    Icon = "bi-keyboard",           Color = "#059669", Category = "System",         Url = "/Transactions" },
            new() { Code = "TCG", Name = "T-Code Generator",     Icon = "bi-cpu",                Color = "#dc2626", Category = "System",         Url = "/TCodeGenerator" },
            new() { Code = "AUD", Name = "Audit Log",            Icon = "bi-journal-text",       Color = "#6b7280", Category = "System",         Url = "/Audit" },
            new() { Code = "ADM", Name = "Administration",       Icon = "bi-gear-wide",          Color = "#6b7280", Category = "System",         Url = "/Admin" },
            new() { Code = "CST", Name = "Customize",            Icon = "bi-sliders",            Color = "#4f46e5", Category = "System",         Url = "/Customization" },
            new() { Code = "INT", Name = "Integration Hub",      Icon = "bi-hdd-rack",           Color = "#0d9488", Category = "System",         Url = "/Integration" },
        };

        QuickActions = new List<QuickAction>
        {
            new() { Name = "Create PO",      Icon = "bi-cart-plus",    Color = "#2563eb", Url = "/MM/PurchaseOrder/Create" },
            new() { Name = "Create SO",      Icon = "bi-file-earmark", Color = "#059669", Url = "/SD/SalesOrder/Create" },
            new() { Name = "Create Sample",  Icon = "bi-flask",        Color = "#7c3aed", Url = "/QM/Sample/Create" },
            new() { Name = "Create Prod Order", Icon = "bi-gear",      Color = "#d97706", Url = "/PP/ProductionOrder/Create" },
            new() { Name = "Create GRN",     Icon = "bi-box-seam",     Color = "#0891b2", Url = "/WM/GRN/Create" },
            new() { Name = "QC Result",      Icon = "bi-clipboard-data", Color = "#dc2626", Url = "/QM/Result/Create" },
        };
    }

    public string CategoryColor(string cat) => cat switch
    {
        "Operations"      => "#2563eb",
        "Finance"         => "#059669",
        "People"          => "#db2777",
        "Projects & Labs" => "#7c3aed",
        "Analytics"       => "#9333ea",
        "System"          => "#6b7280",
        _                 => "#2563eb",
    };

    public string CategoryIcon(string cat) => cat switch
    {
        "Operations"      => "bi-boxes",
        "Finance"         => "bi-calculator",
        "People"          => "bi-people",
        "Projects & Labs" => "bi-flask",
        "Analytics"       => "bi-graph-up",
        "System"          => "bi-gear-wide",
        _                 => "bi-grid-3x3-gap",
    };
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
