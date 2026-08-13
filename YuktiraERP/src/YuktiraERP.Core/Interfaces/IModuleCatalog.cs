using YuktiraERP.Core.Domain.Modules;

namespace YuktiraERP.Core.Interfaces;

public interface IModuleCatalog
{
    IReadOnlyList<ModuleDefinition> Modules { get; }
    ModuleDefinition? GetModule(string code);
    ModuleDefinition? ResolveByRoute(string route);
    IReadOnlyList<string> Categories { get; }
    string CategoryColor(string category);
    string CategoryIcon(string category);
}
