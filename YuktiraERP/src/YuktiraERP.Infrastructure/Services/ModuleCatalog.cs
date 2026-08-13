using YuktiraERP.Core.Domain.Modules;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class ModuleCatalog : IModuleCatalog
{
    public IReadOnlyList<ModuleDefinition> Modules { get; } = new List<ModuleDefinition>
    {
        // ── Operations ──
        new() { Code = "MM", Name = "Materials Management",  Category = "Operations", BaseRoute = "/MM", Icon = "bi-boxes",               Color = "#2563eb" },
        new() { Code = "SD", Name = "Sales & Distribution",  Category = "Operations", BaseRoute = "/SD", Icon = "bi-cart3",              Color = "#059669" },
        new() { Code = "WM", Name = "Warehouse Management",  Category = "Operations", BaseRoute = "/WM", Icon = "bi-house-door",         Color = "#d97706" },
        new() { Code = "PP", Name = "Production Planning",   Category = "Operations", BaseRoute = "/PP", Icon = "bi-gear",               Color = "#7c3aed" },
        new() { Code = "QM", Name = "Quality Management",    Category = "Operations", BaseRoute = "/QM", Icon = "bi-clipboard-check",    Color = "#dc2626" },
        new() { Code = "PM", Name = "Plant Maintenance",     Category = "Operations", BaseRoute = "/PM", Icon = "bi-tools",              Color = "#0891b2" },

        // ── Finance ──
        new() { Code = "FI", Name = "Finance",               Category = "Finance",    BaseRoute = "/FI", Icon = "bi-calculator",         Color = "#059669" },
        new() { Code = "CO", Name = "Controlling",           Category = "Finance",    BaseRoute = "/CO", Icon = "bi-pie-chart",          Color = "#ca8a04" },

        // ── People ──
        new() { Code = "HR", Name = "Human Resources",       Category = "People",     BaseRoute = "/HR", Icon = "bi-people",             Color = "#db2777" },
        new() { Code = "CRM", Name = "Customer Relationship", Category = "People",    BaseRoute = "/CRM", Icon = "bi-person-lines-fill", Color = "#ea580c" },

        // ── Projects & Labs ──
        new() { Code = "PS", Name = "Project System",        Category = "Projects & Labs", BaseRoute = "/PS", Icon = "bi-diagram-3", Color = "#4f46e5" },
        new() { Code = "LIMS", Name = "Lab Information Mgmt", Category = "Projects & Labs", BaseRoute = "/LIMS", Icon = "bi-flask", Color = "#0d9488" },

        // ── Analytics ──
        new() { Code = "BI", Name = "BI Reports",            Category = "Analytics",  BaseRoute = "/BI", Icon = "bi-graph-up",           Color = "#2563eb" },
        new() { Code = "AI", Name = "AI Forecasting",        Category = "Analytics",  BaseRoute = "/PP/Mrp?tab=forecast", Icon = "bi-cpu", Color = "#9333ea", IsSystem = true },

        // ── System ──
        new() { Code = "WF", Name = "Workflows",             Category = "System",     BaseRoute = "/Workflow/Designer", Icon = "bi-arrow-repeat", Color = "#0891b2", IsSystem = true },
        new() { Code = "APP", Name = "Approvals",            Category = "System",     BaseRoute = "/Approval", Icon = "bi-check2-square", Color = "#ca8a04", IsSystem = true },
        new() { Code = "NOT", Name = "Notifications",        Category = "System",     BaseRoute = "/Notifications", Icon = "bi-bell", Color = "#db2777", IsSystem = true },
        new() { Code = "TCD", Name = "Transaction Codes",    Category = "System",     BaseRoute = "/Transactions", Icon = "bi-keyboard", Color = "#059669", IsSystem = true },
        new() { Code = "TCG", Name = "T-Code Generator",     Category = "System",     BaseRoute = "/TCodeGenerator", Icon = "bi-cpu", Color = "#dc2626", IsSystem = true },
        new() { Code = "AUD", Name = "Audit Log",            Category = "System",     BaseRoute = "/Audit", Icon = "bi-journal-text", Color = "#6b7280", IsSystem = true },
        new() { Code = "ADM", Name = "Administration",       Category = "System",     BaseRoute = "/Admin", Icon = "bi-gear-wide", Color = "#6b7280", IsSystem = true },
        new() { Code = "CST", Name = "Customize",            Category = "System",     BaseRoute = "/Customization", Icon = "bi-sliders", Color = "#4f46e5", IsSystem = true },
        new() { Code = "INT", Name = "Integration Hub",      Category = "System",     BaseRoute = "/Integration", Icon = "bi-hdd-rack", Color = "#0d9488", IsSystem = true },
        new() { Code = "PLG", Name = "Plugins",              Category = "System",     BaseRoute = "/Plugins/Manage", Icon = "bi-puzzle", Color = "#6b7280", IsSystem = true },
    };

    public IReadOnlyList<string> Categories { get; } = new[]
    {
        "Operations", "Finance", "People", "Projects & Labs", "Analytics", "System"
    };

    public ModuleDefinition? GetModule(string code)
        => Modules.FirstOrDefault(m => string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase));

    public ModuleDefinition? ResolveByRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return null;
        var seg = route.TrimStart('/').Split('/')[0];
        return Modules.FirstOrDefault(m => string.Equals(m.BaseRoute.TrimStart('/').Split('?')[0].Split('/')[0], seg, StringComparison.OrdinalIgnoreCase));
    }

    public string CategoryColor(string category) => category switch
    {
        "Operations"      => "#2563eb",
        "Finance"         => "#059669",
        "People"          => "#db2777",
        "Projects & Labs" => "#7c3aed",
        "Analytics"       => "#9333ea",
        "System"          => "#6b7280",
        _                 => "#2563eb",
    };

    public string CategoryIcon(string category) => category switch
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
