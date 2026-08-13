using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using YuktiraERP.AIEngine;
using YuktiraERP.Infrastructure;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Hubs;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
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
app.UseMiddleware<TenantMiddleware>();
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
