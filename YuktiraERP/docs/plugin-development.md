# Plugin Development Guide

## Overview

Yuktira ERP supports dynamic plugin loading via the `YuktiraERP.PluginSdk`. Plugins are compiled as standalone class libraries, dropped into the `plugins/` directory, and loaded at runtime by `PluginLoader`. The SDK provides 4 hook interfaces with hot reload, sandboxing, and DB-backed settings/permissions.

**Sample plugins** shipping with the suite:

| Plugin | Code | Hooks Used |
|--------|------|------------|
| AdvancedQC | `ADV_QC` | Startup, Document, Menu |
| DairyExtension | `DAIRY_EXT` | Startup, Menu |
| ExtraReports | `EXTRA_REPORTS` | Startup, Menu |

---

## SDK Reference

The entire SDK lives in `YuktiraERP.PluginSdk`. Reference it from your plugin `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\YuktiraERP.PluginSdk\YuktiraERP.PluginSdk.csproj" />
  </ItemGroup>
</Project>
```

### Core Interface: `IYuktiraPlugin`

Every plugin must implement this interface:

```csharp
public interface IYuktiraPlugin
{
    string Id { get; }             // Unique ID e.g. "yuktira.plugin.advqc"
    string Name { get; }           // Display name
    string Code { get; }           // Short code e.g. "ADV_QC"
    string Version { get; }        // SemVer e.g. "1.0.0"
    string Description { get; }    // Purpose
    string? IconClass { get; }     // Bootstrap icon class
    IEnumerable<string> Dependencies { get; } // Module dependencies (MM, SD, QM, etc.)
    PluginLifecycle Lifecycle { get; }       // Enabled / Disabled / Error
}
```

### Extension Hooks

Plugins opt into features by implementing additional interfaces:

| Interface | Trigger | Use Case |
|-----------|---------|----------|
| `IPluginStartupHook` | Called once when plugin is first loaded per tenant | Init background jobs, warm caches, register routes |
| `IPluginMenuHook` | Called every time the sidebar renders | Add custom menu items |
| `IPluginDocumentHook` | Called when a document is created | Auto-create follow-up docs, enforce rules |
| `IPluginWorkflowHook` | Called at each workflow step | Custom approval logic, conditional routing |

---

## Hook Details

### 1. `IPluginStartupHook`

```csharp
public interface IPluginStartupHook
{
    Task OnStartupAsync(PluginContext context);
}
```

Called once when the plugin loads for a tenant. `PluginContext` provides:

```csharp
public class PluginContext
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, string> Settings { get; set; }
}
```

**Example** — AdvancedQC starts SPC monitoring:

```csharp
public Task OnStartupAsync(PluginContext context)
{
    Console.WriteLine($"[AdvancedQC] SPC monitoring initialized for tenant {context.TenantId}");
    SpcEngine.StartMonitoring(context.TenantId);
    return Task.CompletedTask;
}
```

### 2. `IPluginMenuHook`

```csharp
public interface IPluginMenuHook
{
    List<MenuItem> GetMenuItems(PluginContext context);
}
```

Return a list of `MenuItem` objects merged into the main navigation sidebar.

```csharp
public class MenuItem
{
    public string Label { get; set; }      // Display text
    public string Url { get; set; }        // Route e.g. "/qc/spc"
    public string? Icon { get; set; }      // Bootstrap icon class
    public int Order { get; set; }         // Sort position
    public string? Permission { get; set; } // Optional required permission
    public List<MenuItem>? Children { get; set; } // Nested sub-items
}
```

**Example** — DairyExtension adds 4 menu items:

```csharp
public List<MenuItem> GetMenuItems(PluginContext context)
{
    return new List<MenuItem>
    {
        new() { Label = "Milk Collection", Url = "/dairy/collection",
                Icon = "bi-droplet", Order = 1 },
        new() { Label = "Fat/SNF Testing", Url = "/dairy/testing",
                Icon = "bi-flask", Order = 2 },
        new() { Label = "Procurement", Url = "/dairy/procurement",
                Icon = "bi-truck", Order = 3 },
        new() { Label = "Payout Calculation", Url = "/dairy/payout",
                Icon = "bi-calculator", Order = 4 }
    };
}
```

### 3. `IPluginDocumentHook`

```csharp
public interface IPluginDocumentHook
{
    Task OnDocumentCreateAsync(PluginContext context, string entityType,
        Dictionary<string, object> document);
}
```

Called after a document is created. `entityType` values: `INSPECTION_LOT`, `PURCHASE_ORDER`, `SALES_ORDER`, `INVOICE`, `GOODS_RECEIPT`, etc.

**Example** — AdvancedQC auto-creates SPC charts:

```csharp
public async Task OnDocumentCreateAsync(PluginContext context,
    string entityType, Dictionary<string, object> document)
{
    if (entityType == "INSPECTION_LOT")
    {
        var lotNumber = document.GetValueOrDefault("lot_number");
        await SpcEngine.GenerateControlChart(context.TenantId, lotNumber);
    }
}
```

### 4. `IPluginWorkflowHook`

```csharp
public interface IPluginWorkflowHook
{
    Task<WorkflowAction> OnWorkflowStepAsync(PluginContext context,
        Guid workflowInstanceId, string stepName,
        Dictionary<string, object> data);
}
```

Return one of: `Continue` (proceed), `Pause` (wait), `Skip` (jump step), `Terminate` (fail workflow).

**Example** — Conditional approval:

```csharp
public Task<WorkflowAction> OnWorkflowStepAsync(PluginContext context,
    Guid instanceId, string stepName, Dictionary<string, object> data)
{
    if (stepName == "CreditCheck" && (decimal)data["amount"] > 100000)
        return Task.FromResult(WorkflowAction.Pause);
    return Task.FromResult(WorkflowAction.Continue);
}
```

---

## Building a Plugin

### Step 1 — Create the project

```
src/plugins/MyPlugin/
├── MyPlugin.csproj
└── MyPlugin.cs
```

### Step 2 — Reference the SDK

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\YuktiraERP.PluginSdk\YuktiraERP.PluginSdk.csproj" />
  </ItemGroup>
</Project>
```

### Step 3 — Implement the plugin

```csharp
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Plugins.MyPlugin;

public class MyPlugin : IYuktiraPlugin, IPluginMenuHook
{
    public string Id => "yuktira.plugin.myplugin";
    public string Name => "My Plugin";
    public string Code => "MY_PLUGIN";
    public string Version => "1.0.0";
    public string Description => "Does something useful";
    public string? IconClass => "bi-puzzle";
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    public PluginLifecycle Lifecycle => PluginLifecycle.Enabled;

    public List<MenuItem> GetMenuItems(PluginContext context)
    {
        return new List<MenuItem>
        {
            new() { Label = "My Feature", Url = "/my-plugin",
                    Icon = "bi-puzzle", Order = 10 }
        };
    }
}
```

### Step 4 — Add to solution (optional, for dev)

```bash
dotnet sln YuktiraERP.sln add src/plugins/MyPlugin/MyPlugin.csproj
```

### Step 5 — Build

```bash
dotnet build src/plugins/MyPlugin/MyPlugin.csproj -c Release
```

### Step 6 — Deploy

Copy the built DLL to the `plugins/` folder next to the API binary:

```bash
cp src/plugins/MyPlugin/bin/Release/net10.0/MyPlugin.dll deploy/plugins/
```

**Hot-reload**: `PluginLoader.LoadAll()` scans the directory at startup. Use `POST /api/plugins/{pluginId}/reload` to reload without restarting the server.

---

## Plugin Lifecycle

```
Build → Drop .dll into plugins/ → API starts → PluginLoader.LoadAll()
                                                         ↓
                                              Assembly.LoadFrom(path)
                                                         ↓
                                              Activator.CreateInstance(type)
                                                         ↓
                                              IYuktiraPlugin discovered
                                                         ↓
                                              If IPluginStartupHook → OnStartupAsync()
```

- Plugins with `Lifecycle == PluginLifecycle.Error` or missing dependencies are skipped.
- `PluginLoader.GetPluginHook<T>()` returns the first hook of type `T`.
- `PluginLoader.GetAllPluginHooks<T>()` returns all matching hooks (e.g. all `IPluginMenuHook` instances).

---

## Plugin Manifest

```csharp
public class PluginManifest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string? Description { get; set; }
    public string? AssemblyName { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}
```

Manifests are auto-extracted from assembly metadata or registered manually via `POST /api/plugins/register`.

---

## Database Tables

| Table | Schema | Purpose |
|-------|--------|---------|
| `plugins` | yuktira_plugin | Global plugin registry (code, version, deps, enabled) |
| `plugin_tenant` | yuktira_plugin | Per-tenant enable/disable |
| `plugin_permissions` | yuktira_plugin | Role-based access per plugin |
| `plugin_settings` | yuktira_plugin | Per-tenant key-value settings |

Managed through `IPluginService` interface in Infrastructure layer.

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/plugins` | List all plugins |
| GET | `/api/plugins/{id}/settings` | Get plugin settings |
| POST | `/api/plugins/{id}/settings` | Update plugin settings |
| GET | `/api/plugins/{id}/permissions` | Get plugin permissions |
| GET | `/api/plugins/{id}/status` | Get plugin status (memory, execution stats) |
| POST | `/api/plugins/{id}/reload` | Hot-reload plugin |
| POST | `/api/plugins/{code}/install` | Install plugin |
| DELETE | `/api/plugins/{id}/uninstall` | Uninstall plugin |
| POST | `/api/plugins/{id}/enable` | Enable for tenant |
| POST | `/api/plugins/{id}/disable` | Disable for tenant |

---

## Best Practices

1. **Keep plugins stateless** — use `PluginContext.Settings` for per-tenant configuration
2. **Use async throughout** — all hook methods return `Task`
3. **Handle errors gracefully** — exceptions in hooks are caught and logged; they don't crash the host
4. **Depend on module codes** — `Dependencies` array should reference module codes (`MM`, `SD`, `QM`, etc.)
5. **Prefix menu URLs** — use a unique prefix (e.g. `/dairy/`, `/qc/`) to avoid route conflicts
6. **Avoid DbContext directly** — plugins should call API endpoints or use injected services
7. **Sandboxing**: plugins run in a separate load context to isolate them from the host application
8. **Hot reload**: after dropping a new DLL, call `POST /api/plugins/{id}/reload` — no server restart needed
