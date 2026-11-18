using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.BackgroundServices;

/// <summary>
/// Ensures the yuktira_mes PostgreSQL schema and MES tables exist on startup.
/// </summary>
public class MesSchemaInitializerHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MesSchemaInitializerHostedService> _logger;

    public MesSchemaInitializerHostedService(IServiceScopeFactory scopeFactory, ILogger<MesSchemaInitializerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            if (db.Database.IsRelational())
            {
                await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {MesDbContext.SchemaName};", cancellationToken);

                // Check specifically if yuktira_mes already has tables (HasTablesAsync checks all schemas)
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{MesDbContext.SchemaName}';";
                var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));

                if (count == 0)
                {
                    var creator = db.GetService<IRelationalDatabaseCreator>();
                    await creator.CreateTablesAsync(cancellationToken);
                    _logger.LogInformation("[YuktiraMES] Created tables in schema {Schema}", MesDbContext.SchemaName);
                }
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[YuktiraMES] Schema initialization check skipped/failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
