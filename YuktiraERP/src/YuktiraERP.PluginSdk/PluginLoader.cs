using System.Reflection;

namespace YuktiraERP.PluginSdk;

public class PluginLoader
{
    private readonly Dictionary<string, IYuktiraPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, object> _pluginLoadContexts = new();
    private readonly string _pluginsDirectory;

    public PluginLoader(string? pluginsDirectory = null)
    {
        _pluginsDirectory = pluginsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
    }

    public IReadOnlyDictionary<string, IYuktiraPlugin> LoadedPlugins => _loadedPlugins;
    public IReadOnlyDictionary<string, object> PluginLoadContexts => _pluginLoadContexts;

    public long MaxMemoryPerPlugin { get; set; } = 256L * 1024 * 1024;
    public int MaxExecutionMsPerPlugin { get; set; } = 30000;

    public void LoadAll()
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Directory.CreateDirectory(_pluginsDirectory);
            return;
        }

        foreach (var dllPath in Directory.GetFiles(_pluginsDirectory, "*.dll"))
        {
            LoadPlugin(dllPath);
        }
    }

    public IYuktiraPlugin? LoadPlugin(string assemblyPath)
    {
        try
        {
            var contextKey = Path.GetFileNameWithoutExtension(assemblyPath);
            _pluginLoadContexts[contextKey] = new object();

            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IYuktiraPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is IYuktiraPlugin plugin)
                {
                    _loadedPlugins[plugin.Code] = plugin;
                    Console.WriteLine($"[PluginLoader] Loaded: {plugin.Name} v{plugin.Version}");
                    return plugin;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PluginLoader] Error loading {assemblyPath}: {ex.Message}");
        }

        return null;
    }

    public bool UnloadPlugin(string code)
    {
        if (!_loadedPlugins.ContainsKey(code))
            return false;

        var plugin = _loadedPlugins[code];
        if (plugin is IPluginHotReload hotReload)
        {
            try
            {
                hotReload.OnBeforeReloadAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] Error in OnBeforeReloadAsync for {code}: {ex.Message}");
            }
        }

        _loadedPlugins.Remove(code);
        _pluginLoadContexts.Remove(code);
        Console.WriteLine($"[PluginLoader] Unloaded: {code}");
        return true;
    }

    public IYuktiraPlugin? ReloadPlugin(string code, string newAssemblyPath)
    {
        UnloadPlugin(code);
        return LoadPlugin(newAssemblyPath);
    }

    public async Task<bool> ExecuteSandboxed(Func<Task> action, string pluginCode)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(MaxExecutionMsPerPlugin));
            var task = action();
            await task.WaitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[PluginLoader] Execution timeout for plugin {pluginCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PluginLoader] Error executing plugin {pluginCode}: {ex.Message}");
            return false;
        }
    }

    public T? GetPluginHook<T>() where T : class
    {
        return _loadedPlugins.Values.OfType<T>().FirstOrDefault();
    }

    public List<T> GetAllPluginHooks<T>() where T : class
    {
        return _loadedPlugins.Values.OfType<T>().ToList();
    }

    public IYuktiraPlugin? GetPlugin(string code)
    {
        return _loadedPlugins.GetValueOrDefault(code);
    }

    public bool IsLoaded(string code) => _loadedPlugins.ContainsKey(code);
}
