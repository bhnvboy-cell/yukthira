using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.PluginSdk;

/// <summary>
/// Core contract for all first-party modules and third-party plugins in YuktiraERP.
/// Defines lifecycle, DI registration, Minimal API endpoints, EF Core schema configuration,
/// event subscriptions, and navigation routes.
/// </summary>
public interface IYuktiraModule
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    int Priority { get; }
    IReadOnlyList<string> Dependencies { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);
    void ConfigureModel(ModelBuilder modelBuilder);
    void RegisterEventSubscriptions(IEventBusRegistry registry);
    IReadOnlyList<NavigationRouteDescriptor> GetNavigationRoutes();
}

/// <summary>
/// Metadata descriptor for UI routes and navigation trees provided by a module.
/// </summary>
public class NavigationRouteDescriptor
{
    public string Path { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ModuleGroup { get; set; }
    public string? RequiredPermission { get; set; }
    public int Order { get; set; }
    public List<NavigationRouteDescriptor> Children { get; set; } = new();
}

/// <summary>
/// Handler contract for processing domain events.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry for registering and resolving event handlers in the modular monolith.
/// </summary>
public interface IEventBusRegistry
{
    void Subscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>;

    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>
/// Module registry providing discovery, dependency sorting, and lifecycle management.
/// </summary>
public interface IModuleRegistry
{
    IReadOnlyList<IYuktiraModule> Modules { get; }
    void RegisterModule(IYuktiraModule module);
    IReadOnlyList<IYuktiraModule> GetModulesInPriorityOrder();
}
