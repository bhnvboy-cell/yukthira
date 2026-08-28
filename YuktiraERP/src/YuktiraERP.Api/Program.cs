using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Prometheus;
using Serilog;
using YuktiraERP.Api.Controllers;
using YuktiraERP.Api.GraphQL;
using YuktiraERP.Api.Middleware;
using YuktiraERP.Infrastructure;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.AIEngine;
using YuktiraERP.ExportEngine;
using YuktiraERP.Infrastructure.Hubs;
using YuktiraERP.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<YuktiraDbContext>("database", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var supported = new[] { "en", "hi", "ta", "te", "kn", "ml", "fr", "es" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supported)
        .AddSupportedUICultures(supported)
        .RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
    options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
});

builder.Services.AddControllers(options => options.Conventions.Add(new ApiVersionRouteConvention()));
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Yuktira ERP Suite API", Version = "v1", Description = "Enterprise ERP Platform - Intelligence Driven" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "YuktiraERP",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "YuktiraERPUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperUser", p => p.RequireClaim("IsSuperUser", "true"));
    options.AddPolicy("AdminOrAbove", p => p.RequireRole("SUPER_USER", "ADMIN"));
    options.AddPolicy("PowerUserOrAbove", p => p.RequireRole("SUPER_USER", "ADMIN", "POWER_USER"));
});

builder.Services.AddYuktiraInfrastructure(builder.Configuration);
builder.Services.AddYuktiraAIEngine();
builder.Services.AddYuktiraExportEngine();

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<MaterialMasterType>()
    .AddType<CustomerType>()
    .AddType<VendorType>()
    .AddType<SalesOrderType>()
    .AddType<SalesOrderLineType>()
    .AddType<PurchaseOrderType>()
    .AddType<PurchaseOrderItemType>()
    .AddType<ProductionOrderType>()
    .AddType<StockItemType>()
    .AddType<BatchType>()
    .AddType<QualityNotificationType>()
    .AddType<UniversalJournalType>()
    .AddType<StockMovementType>()
    .AddType<InspectionLotType>()
    .AddType<MaintenanceOrderType>()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

// Dashboard Hub
builder.Services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5001", "http://127.0.0.1:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();

    // Load plugins from the plugins/ folder (logs & activates IYuktiraPlugin assemblies)
    var loader = scope.ServiceProvider.GetRequiredService<YuktiraERP.PluginSdk.PluginLoader>();
    try { loader.LoadAll(); }
    catch (Exception ex) { System.Console.WriteLine($"[PluginLoader] failed to load plugins: {ex.Message}"); }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TenantId", http.Items.ContainsKey("TenantId") ? http.Items["TenantId"]?.ToString() : null);
        diag.Set("Path", http.Request.Path.Value);
    };
});
app.UseSecurityHeaders();
app.UseHttpMetrics();
app.UseMiddleware<ApiThrottlingMiddleware>(builder.Configuration.GetValue<int>("Throttling:MaxRequestsPerMinute", 100));
app.UseRequestLocalization();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseCors("AllowWebApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            database = report.Entries.TryGetValue("database", out var db) ? db.Status.ToString() : "Unknown",
            timestamp = DateTime.UtcNow
        });
    }
});
app.MapMetrics();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapGraphQL("/api/graphql");

app.Run();
