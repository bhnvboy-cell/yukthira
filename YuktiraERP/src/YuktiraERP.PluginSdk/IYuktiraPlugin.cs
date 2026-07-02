namespace YuktiraERP.PluginSdk;

public interface IYuktiraPlugin
{
    string Id { get; }
    string Name { get; }
    string Code { get; }
    string Version { get; }
    string Description { get; }
    string? IconClass { get; }
    IEnumerable<string> Dependencies { get; }
    PluginLifecycle Lifecycle { get; }
}

public enum PluginLifecycle { Enabled, Disabled, Error }

public interface IPluginStartupHook
{
    Task OnStartupAsync(PluginContext context);
}

public interface IPluginMenuHook
{
    List<MenuItem> GetMenuItems(PluginContext context);
}

public interface IPluginDocumentHook
{
    Task OnDocumentCreateAsync(PluginContext context, string entityType, Dictionary<string, object> document);
}

public interface IPluginWorkflowHook
{
    Task<WorkflowAction> OnWorkflowStepAsync(PluginContext context, Guid workflowInstanceId, string stepName, Dictionary<string, object> data);
}

public enum WorkflowAction { Continue, Pause, Skip, Terminate }

public class PluginContext
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class MenuItem
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int Order { get; set; }
    public string? Permission { get; set; }
    public List<MenuItem>? Children { get; set; }
}

public class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string? Description { get; set; }
    public string? AssemblyName { get; set; }
    public string? IconClass { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string MinVersion { get; set; } = "1.0.0";
    public bool IsCore { get; set; }
}

public interface IPluginConfigurable
{
    List<PluginSettingDefinition> GetSettingDefinitions();
    Task ApplySettingsAsync(Dictionary<string, string> settings);
}

public interface IPluginPermissionProvider
{
    List<PluginPermission> GetPermissions();
}

public interface IPluginSandboxed
{
    string[] AllowedAssemblyPrefixes { get; }
    string[] AllowedNamespaces { get; }
    long MaxMemoryBytes { get; }
    int MaxExecutionMs { get; }
}

public interface IPluginHotReload
{
    Task OnBeforeReloadAsync();
    Task OnAfterReloadAsync();
    bool CanHotReload { get; }
}

public class PluginSettingDefinition
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "string";
    public string DefaultValue { get; set; } = "";
    public List<string>? Options { get; set; }
    public string Description { get; set; } = "";
    public bool IsRequired { get; set; }
}

public class PluginPermission
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string Group { get; set; } = "General";
}
