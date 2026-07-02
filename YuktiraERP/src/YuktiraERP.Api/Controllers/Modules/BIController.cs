using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/bi")]
[Authorize]
public class BIController : ControllerBase
{
    private readonly ITenantContext _tenant;
    private readonly YuktiraDbContext _db;
    private readonly IKpiService _kpiService;

    public BIController(ITenantContext tenant, YuktiraDbContext db, IKpiService kpiService)
    {
        _tenant = tenant;
        _db = db;
        _kpiService = kpiService;
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        var reports = await _db.BIReports
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.ReportName, r.Category, r.Format, r.ChartType, r.Query, r.FilterJson, r.LastRun, r.CreatedBy, r.CreatedAt
            })
            .ToListAsync();
        return Ok(new { data = reports, tenantId = _tenant.TenantId });
    }

    [HttpPost("reports")]
    public async Task<IActionResult> CreateReport([FromBody] CreateBiReportRequest request)
    {
        var report = new BIReportEntity
        {
            ReportName = request.ReportName,
            Category = request.Category ?? "",
            Format = request.Format ?? "HTML",
            Query = request.Query ?? "",
            ChartType = request.ChartType ?? "bar",
            FilterJson = request.FilterJson ?? "{}",
            CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ""
        };
        _db.BIReports.Add(report);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = report.Id, tenantId = _tenant.TenantId });
    }

    [HttpGet("reports/{id:guid}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await _db.BIReports.FindAsync(id);
        if (report == null) return NotFound();
        return Ok(report);
    }

    [HttpDelete("reports/{id:guid}")]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        var report = await _db.BIReports.FindAsync(id);
        if (report == null) return NotFound();
        _db.BIReports.Remove(report);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("reports/{id:guid}/run")]
    public async Task<IActionResult> RunReport(Guid id)
    {
        var report = await _db.BIReports.FindAsync(id);
        if (report == null) return NotFound();

        var result = new List<Dictionary<string, object>>();
        if (!string.IsNullOrEmpty(report.Query))
        {
            try
            {
                using var cmd = _db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = report.Query;
                var tenantParam = cmd.CreateParameter();
                tenantParam.ParameterName = "tenant";
                tenantParam.Value = _tenant.TenantId;
                cmd.Parameters.Add(tenantParam);
                await _db.Database.OpenConnectionAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.GetValue(i);
                    result.Add(row);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Report execution failed", detail = ex.Message });
            }
        }

        report.LastRun = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { data = result, chartType = report.ChartType, tenantId = _tenant.TenantId });
    }

    [HttpGet("dashboards")]
    public async Task<IActionResult> GetDashboards()
    {
        var dashboards = await _db.Dashboards
            .Where(d => d.Status == "Active")
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return Ok(new { data = dashboards, tenantId = _tenant.TenantId });
    }

    [HttpPost("dashboards")]
    public async Task<IActionResult> CreateDashboard([FromBody] CreateBiDashboardRequest request)
    {
        var dashboard = new DashboardEntity
        {
            DashboardId = request.DashboardId ?? Guid.NewGuid().ToString("N")[..8],
            Name = request.Name,
            Category = request.Category ?? "General",
            ConfigJson = request.ConfigJson ?? "{}",
            Status = "Active"
        };
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = dashboard.Id, tenantId = _tenant.TenantId });
    }

    [HttpGet("dashboards/{id:guid}")]
    public async Task<IActionResult> GetDashboard(Guid id)
    {
        var dashboard = await _db.Dashboards.FindAsync(id);
        if (dashboard == null) return NotFound();
        return Ok(new { data = dashboard, tenantId = _tenant.TenantId });
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetAvailableKpis()
    {
        var kpis = await _kpiService.GetAvailableKpisAsync(_tenant.TenantId);
        return Ok(new { data = kpis, tenantId = _tenant.TenantId });
    }

    [HttpPost("kpis/calculate")]
    public async Task<IActionResult> CalculateKpi([FromBody] CalculateKpiRequest request)
    {
        var result = await _kpiService.CalculateKpiAsync(_tenant.TenantId, request.KpiCode);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}

public class CreateBiReportRequest
{
    public string ReportName { get; set; } = "";
    public string? Category { get; set; }
    public string? Format { get; set; }
    public string? Query { get; set; }
    public string? ChartType { get; set; }
    public string? FilterJson { get; set; }
}

public class CreateBiDashboardRequest
{
    public string Name { get; set; } = "";
    public string? DashboardId { get; set; }
    public string? Category { get; set; }
    public string? ConfigJson { get; set; }
}

public class CalculateKpiRequest
{
    public string KpiCode { get; set; } = "";
}
