using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.PluginSdk;

public class ModuleRegistry : IModuleRegistry
{
    private readonly List<IYuktiraModule> _modules = new();
    private readonly ILogger<ModuleRegistry>? _logger;

    public ModuleRegistry(ILogger<ModuleRegistry>? logger = null)
    {
        _logger = logger;
    }

    public IReadOnlyList<IYuktiraModule> Modules => _modules.AsReadOnly();

    public void RegisterModule(IYuktiraModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        if (_modules.Any(m => m.Id.Equals(module.Id, StringComparison.OrdinalIgnoreCase)))
        {
            _logger?.LogWarning("Module '{ModuleId}' is already registered. Skipping duplicate.", module.Id);
            return;
        }

        _modules.Add(module);
        _logger?.LogInformation("Registered module '{ModuleName}' (Id: {ModuleId}, Version: {Version}, Priority: {Priority})",
            module.Name, module.Id, module.Version, module.Priority);
    }

    /// <summary>
    /// Returns registered modules topologically sorted by declared dependencies and priority order.
    /// </summary>
    public IReadOnlyList<IYuktiraModule> GetModulesInPriorityOrder()
    {
        var result = new List<IYuktiraModule>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sort initially by Priority ascending (lower number runs earlier)
        var sortedByPriority = _modules.OrderBy(m => m.Priority).ToList();

        void Visit(IYuktiraModule mod)
        {
            if (inStack.Contains(mod.Id))
                throw new InvalidOperationException($"Circular dependency detected involving module '{mod.Id}'.");

            if (visited.Contains(mod.Id))
                return;

            inStack.Add(mod.Id);

            foreach (var depId in mod.Dependencies)
            {
                var depModule = _modules.FirstOrDefault(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));
                if (depModule != null)
                {
                    Visit(depModule);
                }
            }

            inStack.Remove(mod.Id);
            visited.Add(mod.Id);
            result.Add(mod);
        }

        foreach (var mod in sortedByPriority)
        {
            Visit(mod);
        }

        return result.AsReadOnly();
    }
}

public class ModuleLoader
{
    private readonly ModuleRegistry _registry;
    private readonly ILogger<ModuleLoader>? _logger;

    public ModuleLoader(ModuleRegistry registry, ILogger<ModuleLoader>? logger = null)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Scans provided assemblies for implementations of IYuktiraModule and registers them.
    /// </summary>
    public void DiscoverModules(IEnumerable<Assembly> assemblies)
    {
        foreach (var asm in assemblies)
        {
            try
            {
                var moduleTypes = asm.GetTypes()
                    .Where(t => typeof(IYuktiraModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in moduleTypes)
                {
                    if (Activator.CreateInstance(type) is IYuktiraModule module)
                    {
                        _registry.RegisterModule(module);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed scanning assembly '{AssemblyName}' for Yuktira modules.", asm.FullName);
            }
        }
    }

    /// <summary>
    /// Boots services across discovered modules.
    /// </summary>
    public void BootstrapServices(IServiceCollection services, IConfiguration configuration)
    {
        var ordered = _registry.GetModulesInPriorityOrder();
        foreach (var module in ordered)
        {
            _logger?.LogInformation("Registering services for module: {ModuleId}", module.Id);
            module.RegisterServices(services, configuration);
        }
    }

    /// <summary>
    /// Maps endpoints across discovered modules.
    /// </summary>
    public void BootstrapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var ordered = _registry.GetModulesInPriorityOrder();
        foreach (var module in ordered)
        {
            _logger?.LogInformation("Configuring endpoints for module: {ModuleId}", module.Id);
            module.ConfigureEndpoints(endpoints);
        }
    }

    /// <summary>
    /// Applies EF Core configurations across discovered modules.
    /// </summary>
    public void BootstrapModel(ModelBuilder modelBuilder)
    {
        var ordered = _registry.GetModulesInPriorityOrder();
        foreach (var module in ordered)
        {
            _logger?.LogInformation("Configuring EF Core model for module: {ModuleId}", module.Id);
            module.ConfigureModel(modelBuilder);
        }
    }
}

public class EventBusRegistry : IEventBusRegistry
{
    private readonly Dictionary<Type, List<Type>> _subscriptions = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusRegistry>? _logger;

    public EventBusRegistry(IServiceProvider serviceProvider, ILogger<EventBusRegistry>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Type>();
            _subscriptions[eventType] = handlers;
        }

        if (!handlers.Contains(typeof(THandler)))
        {
            handlers.Add(typeof(THandler));
        }
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventType = domainEvent.GetType();
        if (!_subscriptions.TryGetValue(eventType, out var handlerTypes))
        {
            _logger?.LogDebug("No handlers registered for event {EventType}", eventType.Name);
            return;
        }

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(handlerType);
                if (handler == null)
                {
                    handler = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
                }

                if (handler is IDomainEventHandler<TEvent> typedHandler)
                {
                    await typedHandler.HandleAsync(domainEvent, cancellationToken);
                }
                else
                {
                    // Fallback using reflection if covariance is needed
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<TEvent>.HandleAsync));
                    if (method != null)
                    {
                        var task = (Task?)method.Invoke(handler, new object[] { domainEvent, cancellationToken });
                        if (task != null) await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing handler {HandlerType} for event {EventType}", handlerType.Name, eventType.Name);
                throw;
            }
        }
    }
}
