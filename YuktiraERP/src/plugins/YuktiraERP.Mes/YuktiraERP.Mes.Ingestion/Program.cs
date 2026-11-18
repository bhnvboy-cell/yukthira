using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YuktiraERP.Mes.Infrastructure;
using YuktiraERP.Mes.Ingestion;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddYuktiraMesInfrastructure(builder.Configuration);
builder.Services.AddSingleton<TelemetryIngestionWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelemetryIngestionWorker>());

var host = builder.Build();
await host.RunAsync();
