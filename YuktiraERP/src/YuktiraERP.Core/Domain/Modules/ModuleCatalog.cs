namespace YuktiraERP.Core.Domain.Modules;

public sealed class ModuleDefinition
{
    public required string Code { get; init; }          // canonical module code, e.g. "MM", "SD"
    public required string Name { get; init; }          // display name
    public required string Category { get; init; }      // dashboard/sidebar grouping
    public required string BaseRoute { get; init; }     // module landing page route, e.g. "/MM"
    public required string Icon { get; init; }          // bootstrap icon class
    public required string Color { get; init; }         // hex accent color
    public bool IsSystem { get; init; }                 // meta module (admin/tooling), not a business line
}

public sealed class ModuleCatalogEntry
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }         // SAP-style transaction group
    public required string Route { get; init; }
    public required string Icon { get; init; }
    public required string RequiredRole { get; init; }
}
