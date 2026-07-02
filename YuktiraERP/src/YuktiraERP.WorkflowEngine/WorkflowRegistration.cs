using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.WorkflowEngine;

public static class WorkflowRegistration
{
    public static IServiceCollection AddYuktiraWorkflowEngine(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowEngine, WorkflowEngineService>();
        return services;
    }
}
