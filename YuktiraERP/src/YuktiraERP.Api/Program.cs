using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using YuktiraERP.Api.Middleware;
using YuktiraERP.Infrastructure;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.AIEngine;
using YuktiraERP.ExportEngine;
using YuktiraERP.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

app.UseMiddleware<ApiThrottlingMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
