using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.AIEngine;

public static class AIEngineRegistration
{
    public static IServiceCollection AddYuktiraAIEngine(this IServiceCollection services)
    {
        services.AddSingleton<IAIEngine, PredictionEngine>();
        return services;
    }
}
