using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Serilog;
using YuktiraERP.AIEngine;
using YuktiraERP.Infrastructure;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Hubs;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "web-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<YuktiraDbContext>("database", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var supported = new[] { "en", "hi", "ta", "te", "kn", "ml", "fr", "es" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supported)
        .AddSupportedUICultures(supported);
});
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddSignalR();
builder.Services.AddYuktiraInfrastructure(builder.Configuration);
builder.Services.AddYuktiraAIEngine();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperUser", p => p.RequireClaim("IsSuperUser", "true"));
    options.AddPolicy("AdminOrAbove", p => p.RequireRole("SUPER_USER", "ADMIN"));
    options.AddPolicy("PowerUserOrAbove", p => p.RequireRole("SUPER_USER", "ADMIN", "POWER_USER"));
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
});

builder.WebHost.UseUrls("http://0.0.0.0:5001");

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

app.UseSecurityHeaders();
app.UseSerilogRequestLogging();
app.UseMiddleware<TenantMiddleware>();
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ModuleAuthorizationMiddleware>();

app.MapRazorPages();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public class ModuleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] AdminModules = ["Admin", "Audit", "Integration", "Plugins", "TCode", "TCodeGenerator", "Customization"];
    private static readonly HashSet<string> PowerUserPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/BI/Dashboard/Create",
        "/BI/Report/Create",
        "/Workflow/Instances",
        "/Workflow/Designer"
    };

    public ModuleAuthorizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (path.StartsWith("/Pages/") || path.StartsWith("/"))
        {
            var route = path.TrimStart('/').TrimEnd('/');
            var topFolder = route.Split('/')[0];

            if (topFolder != "Auth" && topFolder != "health" && context.User?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = 401;
                return;
            }

            if (AdminModules.Contains(topFolder, StringComparer.OrdinalIgnoreCase))
            {
                if (context.User?.IsInRole("SUPER_USER") != true && context.User?.IsInRole("ADMIN") != true)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }
            else if (PowerUserPages.Contains("/" + route))
            {
                if (context.User?.IsInRole("SUPER_USER") != true &&
                    context.User?.IsInRole("ADMIN") != true &&
                    context.User?.IsInRole("POWER_USER") != true)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }
        }

        await _next(context);
    }
}
