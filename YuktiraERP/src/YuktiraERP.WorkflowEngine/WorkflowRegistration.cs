using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.WorkflowEngine;

public static class WorkflowRegistration
{
    [Obsolete("Legacy in-memory engine is not recommended. The DB-backed WorkflowService is the production implementation (registered in InfrastructureRegistration).")]
    public static IServiceCollection AddYuktiraWorkflowEngine(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowEngine, WorkflowEngineService>();
        return services;
    }
}
