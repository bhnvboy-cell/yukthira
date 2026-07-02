using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Transaction;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class TransactionCodeService : ITransactionCodeService
{
    private readonly YuktiraDbContext _db;
    private static readonly Dictionary<string, string> RouteMap = new()
    {
        // MM - Materials Management
        ["MM01"] = "/MM/Material/Create", ["MM02"] = "/MM/Material/Edit", ["MM03"] = "/MM/Material/Display",
        ["MMBE"] = "/MM/StockOverview", ["ME11"] = "/MM/Vendor/Create", ["ME12"] = "/MM/Vendor/Edit",
        ["ME13"] = "/MM/Vendor/Display", ["ME21N"] = "/MM/PO/Create", ["ME22N"] = "/MM/PO/Edit",
        ["ME23N"] = "/MM/PO/Display",         ["MIGO"] = "/MM/GoodsReceipt", ["MIRO"] = "/MM/InvoiceVerification/Create",
        ["MB52"] = "/MM/StockList", ["MB1A"] = "/MM/GoodsIssue", ["MB1C"] = "/MM/GoodsReceiptOther",
        // SD - Sales & Distribution
        ["VA01"] = "/SD/SalesOrder/Create", ["VA02"] = "/SD/SalesOrder/Edit", ["VA03"] = "/SD/SalesOrder/Display",
        ["VA05"] = "/SD/SalesOrder/List", ["VLO1N"] = "/SD/Delivery/Create", ["VF01"] = "/SD/Billing/Create",
        ["VD01"] = "/SD/Customer/Create", ["VD02"] = "/SD/Customer/Edit", ["VD03"] = "/SD/Customer/Display",
        ["VKD1"] = "/SD/Customer/List",
        // PP - Production Planning
        ["CS01"] = "/PP/BOM/Create", ["CS02"] = "/PP/BOM/Edit", ["CS03"] = "/PP/BOM/Display",
        ["CO01"] = "/PP/ProductionOrder/Create", ["CO02"] = "/PP/ProductionOrder/Edit", ["CO03"] = "/PP/ProductionOrder/Display",
        ["MD01"] = "/PP/MrpRun", ["MD04"] = "/PP/MrpStock", ["CR01"] = "/PP/WorkCenter/Create",
        // QM - Quality Management
        ["QE01"] = "/QM/InspectionLot/Create", ["QE02"] = "/QM/InspectionLot/Edit", ["QE03"] = "/QM/InspectionLot/Display",
        ["QS01"] = "/QM/InspectionPlan/Create", ["QS02"] = "/QM/InspectionPlan/Edit",
        ["QA01"] = "/QM/InspectionResult/Create", ["QUD"] = "/QM/UsageDecision/Create",
        // WM - Warehouse
        ["LT01"] = "/WM/Transfer/Create", ["LT02"] = "/WM/Transfer/Edit", ["LT03"] = "/WM/Transfer/Display",
        ["LS01"] = "/WM/StorageLocation/Create", ["LS02"] = "/WM/StorageLocation/Edit",
        // FI - Finance
        ["FB50"] = "/FI/Ledger/Create", ["FB60"] = "/FI/AP/InvoiceCreate", ["FB70"] = "/FI/AR/InvoiceCreate",
        ["FBL1N"] = "/FI/AP/List", ["FBL5N"] = "/FI/AR/List", ["FS10N"] = "/FI/GL/Display",
        ["FAGLL03"] = "/FI/GL/LineItems", ["F.01"] = "/FI/Reports/BalanceSheet",
        ["F.02"] = "/FI/Reports/ProfitLoss", ["F-03"] = "/FI/AP/Payment", ["F-28"] = "/FI/AR/Payment",
        // CO - Controlling
        ["KA01"] = "/CO/CostCenter/Create", ["KA02"] = "/CO/CostCenter/Edit", ["KA03"] = "/CO/CostCenter/Display",
        ["KOB1"] = "/CO/CostCenterReport",
        // HR - Human Resources
        ["PA30"] = "/HR/Employee/Edit", ["PA20"] = "/HR/Employee/Display",
        ["PA40"] = "/HR/EmployeeList", ["PT60"] = "/HR/AttendanceReport",
        ["PR01"] = "/HR/PayrollRun", ["PR05"] = "/HR/PayrollHistory",
        // CRM
        ["CRM01"] = "/CRM/Lead/Create", ["CRM02"] = "/CRM/Lead/Edit", ["CRM03"] = "/CRM/Lead/Display",
        ["CRM04"] = "/CRM/Opportunity/Create", ["CRM05"] = "/CRM/Opportunity/Edit",
        ["CRM06"] = "/CRM/PipelineReport",
        // LIMS
        ["LM01"] = "/LIMS/Sample/Create", ["LM02"] = "/LIMS/Sample/Edit", ["LM03"] = "/LIMS/Sample/Display",
        ["LM04"] = "/LIMS/TestResult/Create", ["LM05"] = "/LIMS/Instrument/Create",
        // BI - Business Intelligence
        ["BI01"] = "/BI/Report/Create", ["BI02"] = "/BI/ReportRun", ["BI03"] = "/BI/DashboardDisplay",
        ["BI04"] = "/BI/KpiMonitor",
        // System
        ["SU01"] = "/Admin/Users", ["SU02"] = "/Admin/Tenants", ["SU03"] = "/Admin/SystemConfig",
        ["AL01"] = "/Audit", ["AL02"] = "/Audit/Suspicious",
        ["WF01"] = "/Workflow/Designer", ["WF02"] = "/Workflow/Instances",
        ["AP01"] = "/Approval/Pending", ["AP02"] = "/Approval/History",
        ["NO01"] = "/Notifications/Inbox",
        ["TC01"] = "/Transactions/Launcher", ["TC02"] = "/Transactions/Manage",
        ["PL01"] = "/Plugins/Manage"
    };

    public TransactionCodeService(YuktiraDbContext db) => _db = db;

    public async Task<List<TransactionCodeDto>> GetAllAsync(string? module = null, TransactionGroup? group = null, string? search = null)
    {
        await EnsureSeedAsync();
        var query = _db.TransactionCodes.AsQueryable();
        if (!string.IsNullOrEmpty(module)) query = query.Where(t => t.Module == module);
        if (group.HasValue) query = query.Where(t => t.GroupName == group.ToString());
        if (!string.IsNullOrEmpty(search)) query = query.Where(t => t.Code.Contains(search) || t.Name.Contains(search) || t.Description.Contains(search));
        return (await query.OrderBy(t => t.SortOrder).ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<TransactionCodeDto?> GetByIdAsync(Guid id)
    {
        await EnsureSeedAsync();
        var entity = await _db.TransactionCodes.FindAsync(id);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<TransactionCodeDto?> GetByCodeAsync(string code)
    {
        await EnsureSeedAsync();
        var entity = await _db.TransactionCodes.FirstOrDefaultAsync(t => t.Code == code);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<TransactionCodeDto> CreateAsync(TransactionCodeDto dto)
    {
        var entity = new TransactionCodeEntity
        {
            Code = dto.Code.ToUpperInvariant(),
            Name = dto.Name,
            Description = dto.Description,
            Module = dto.Module,
            GroupName = dto.Group.ToString(),
            Route = dto.Route,
            Icon = dto.Icon,
            SortOrder = dto.SortOrder,
            Status = dto.Status.ToString(),
            IsSystem = dto.IsSystem,
            RequiredRole = dto.RequiredRole,
            Params = dto.Params
        };
        _db.TransactionCodes.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        dto.CreatedAt = entity.CreatedAt;
        return dto;
    }

    public async Task<TransactionCodeDto?> UpdateAsync(Guid id, TransactionCodeDto dto)
    {
        var entity = await _db.TransactionCodes.FindAsync(id);
        if (entity is null) return null;
        entity.Code = dto.Code.ToUpperInvariant();
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Module = dto.Module;
        entity.GroupName = dto.Group.ToString();
        entity.Route = dto.Route;
        entity.Icon = dto.Icon;
        entity.SortOrder = dto.SortOrder;
        entity.Status = dto.Status.ToString();
        entity.RequiredRole = dto.RequiredRole;
        entity.Params = dto.Params;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _db.TransactionCodes.FindAsync(id);
        if (entity is null || entity.IsSystem) return false;
        _db.TransactionCodes.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TransactionCodeDto>> SearchAsync(string query)
    {
        await EnsureSeedAsync();
        var q = query.ToUpperInvariant();
        return (await _db.TransactionCodes
            .Where(t => t.Code.Contains(q) || t.Name.Contains(q))
            .OrderBy(t => t.SortOrder)
            .Take(20)
            .ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<ExecuteTransactionResult> ExecuteAsync(string code, Guid? userId, Guid? tenantId, string? ipAddress, Dictionary<string, object>? parameters = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await EnsureSeedAsync();
            var entity = await _db.TransactionCodes.FirstOrDefaultAsync(t => t.Code == code.ToUpperInvariant());
            if (entity is null)
            {
                sw.Stop();
                await LogExecutionAsync(code, userId, tenantId, ipAddress, ExecutionStatus.NotFound, sw.ElapsedMilliseconds, error: $"Transaction code '{code}' not found");
                return new ExecuteTransactionResult { Status = ExecutionStatus.NotFound, Message = $"Transaction code '{code}' not found", DurationMs = sw.ElapsedMilliseconds };
            }

            if (entity.Status != "Active")
            {
                sw.Stop();
                await LogExecutionAsync(code, userId, tenantId, ipAddress, ExecutionStatus.Failed, sw.ElapsedMilliseconds, error: "Transaction is inactive");
                return new ExecuteTransactionResult { Status = ExecutionStatus.Failed, Message = "Transaction is inactive", DurationMs = sw.ElapsedMilliseconds };
            }

            var route = entity.Route;
            if (!string.IsNullOrEmpty(route) && parameters?.ContainsKey("id") == true)
            {
                route = $"{route}/{parameters["id"]}";
            }

            sw.Stop();
            await LogExecutionAsync(code, userId, tenantId, ipAddress, ExecutionStatus.Success, sw.ElapsedMilliseconds, requestData: parameters?.ToString());
            return new ExecuteTransactionResult
            {
                Status = ExecutionStatus.Success,
                RedirectUrl = route,
                Message = $"Transaction '{entity.Name}' executed successfully",
                DurationMs = sw.ElapsedMilliseconds,
                Data = new { entity.Code, entity.Name, entity.Module }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            await LogExecutionAsync(code, userId, tenantId, ipAddress, ExecutionStatus.Failed, sw.ElapsedMilliseconds, error: ex.Message);
            return new ExecuteTransactionResult { Status = ExecutionStatus.Failed, Message = ex.Message, DurationMs = sw.ElapsedMilliseconds };
        }
    }

    public async Task<List<TransactionCodeDto>> GetFavoritesAsync(Guid userId)
    {
        await EnsureSeedAsync();
        var favCodes = await _db.TransactionPermissions
            .Where(p => p.PrincipalType == "User" && p.PrincipalValue == userId.ToString() && p.IsFavorite)
            .Select(p => p.TransactionCodeId)
            .ToListAsync();
        return (await _db.TransactionCodes
            .Where(t => favCodes.Contains(t.Id))
            .OrderBy(t => t.Code)
            .ToListAsync()).Select(ToDto).ToList();
    }

    public async Task ToggleFavoriteAsync(Guid userId, Guid transactionCodeId)
    {
        var existing = await _db.TransactionPermissions
            .FirstOrDefaultAsync(p => p.PrincipalType == "User" && p.PrincipalValue == userId.ToString() && p.TransactionCodeId == transactionCodeId);
        if (existing is not null)
        {
            existing.IsFavorite = !existing.IsFavorite;
        }
        else
        {
            _db.TransactionPermissions.Add(new TransactionPermissionEntity
            {
                TransactionCodeId = transactionCodeId,
                PrincipalType = "User",
                PrincipalValue = userId.ToString(),
                CanAccess = true,
                IsFavorite = true
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<TransactionCodeDto>> GetRecentAsync(Guid userId, int count = 10)
    {
        await EnsureSeedAsync();
        var recentCodes = await _db.TransactionLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.TransactionCode)
            .Distinct()
            .Take(count)
            .ToListAsync();
        return (await _db.TransactionCodes
            .Where(t => recentCodes.Contains(t.Code))
            .ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<List<TransactionCodeDto>> GetPermittedCodesAsync(Guid? userId, string? role)
    {
        await EnsureSeedAsync();
        var deniedCodes = await _db.TransactionPermissions
            .Where(p => !p.CanAccess && ((p.PrincipalType == "Role" && p.PrincipalValue == role) || (p.PrincipalType == "User" && p.PrincipalValue == userId.ToString())))
            .Select(p => p.TransactionCodeId)
            .ToListAsync();
        return (await _db.TransactionCodes
            .Where(t => t.Status == "Active" && !deniedCodes.Contains(t.Id))
            .OrderBy(t => t.SortOrder)
            .ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<bool> ValidateAccessAsync(string code, Guid? userId, string? role)
    {
        var entity = await _db.TransactionCodes.FirstOrDefaultAsync(t => t.Code == code.ToUpperInvariant());
        if (entity is null) return false;
        var denied = await _db.TransactionPermissions.AnyAsync(p =>
            p.TransactionCodeId == entity.Id && !p.CanAccess &&
            ((p.PrincipalType == "Role" && p.PrincipalValue == role) || (p.PrincipalType == "User" && p.PrincipalValue == userId.ToString())));
        return !denied;
    }

    public async Task<TransactionPermissionDto?> SetPermissionAsync(TransactionPermissionDto dto)
    {
        var existing = await _db.TransactionPermissions
            .FirstOrDefaultAsync(p => p.TransactionCodeId == dto.TransactionCodeId && p.PrincipalType == dto.PrincipalType && p.PrincipalValue == dto.PrincipalValue);
        if (existing is not null)
        {
            existing.CanAccess = dto.CanAccess;
        }
        else
        {
            var entity = new TransactionPermissionEntity
            {
                TransactionCodeId = dto.TransactionCodeId,
                PrincipalType = dto.PrincipalType,
                PrincipalValue = dto.PrincipalValue,
                CanAccess = dto.CanAccess
            };
            _db.TransactionPermissions.Add(entity);
            dto.Id = entity.Id;
        }
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task<List<TransactionPermissionDto>> GetPermissionsAsync(Guid transactionCodeId)
    {
        return await _db.TransactionPermissions
            .Where(p => p.TransactionCodeId == transactionCodeId)
            .Select(p => new TransactionPermissionDto
            {
                Id = p.Id,
                TransactionCodeId = p.TransactionCodeId,
                PrincipalType = p.PrincipalType,
                PrincipalValue = p.PrincipalValue,
                CanAccess = p.CanAccess
            })
            .ToListAsync();
    }

    public async Task<List<TransactionLogDto>> GetLogAsync(Guid? userId = null, string? code = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        var query = _db.TransactionLogs.AsQueryable();
        if (userId.HasValue) query = query.Where(l => l.UserId == userId);
        if (!string.IsNullOrEmpty(code)) query = query.Where(l => l.TransactionCode == code);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);
        var raw = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return raw.Select(l => new TransactionLogDto
        {
            Id = l.Id,
            TransactionCode = l.TransactionCode,
            UserName = l.UserName,
            Status = Enum.TryParse<ExecutionStatus>(l.Status, out var s) ? s : ExecutionStatus.Failed,
            IpAddress = l.IpAddress,
            DurationMs = l.DurationMs,
            ErrorMessage = l.ErrorMessage,
            Timestamp = l.CreatedAt
        }).ToList();
    }

    private async Task LogExecutionAsync(string code, Guid? userId, Guid? tenantId, string? ipAddress, ExecutionStatus status, long durationMs, string? error = null, string? requestData = null)
    {
        _db.TransactionLogs.Add(new TransactionLogEntity
        {
            TransactionCodeId = Guid.Empty,
            TransactionCode = code,
            UserId = userId,
            UserName = userId.HasValue ? (await _db.AdminUsers.FindAsync(userId.Value))?.UserName ?? "" : "",
            TenantId = tenantId,
            Status = status.ToString(),
            IpAddress = ipAddress ?? "",
            DurationMs = durationMs,
            ErrorMessage = error,
            RequestData = requestData
        });
        await _db.SaveChangesAsync();
    }

    private static TransactionCodeDto ToDto(TransactionCodeEntity e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name, Description = e.Description,
        Module = e.Module,
        Group = Enum.TryParse<TransactionGroup>(e.GroupName, out var g) ? g : TransactionGroup.Transactions,
        Route = e.Route, Icon = e.Icon, SortOrder = e.SortOrder,
        Status = Enum.TryParse<TransactionStatus>(e.Status, out var s) ? s : TransactionStatus.Active,
        IsSystem = e.IsSystem, RequiredRole = e.RequiredRole, Params = e.Params, CreatedAt = e.CreatedAt
    };

    private static readonly Func<TransactionCodeEntity, TransactionCodeDto> ToDtoFunc = ToDto;

    private bool _seeded;
    private readonly SemaphoreSlim _seedLock = new(1, 1);

    private async Task EnsureSeedAsync()
    {
        if (_seeded) return;
        await _seedLock.WaitAsync();
        try
        {
            if (_seeded) return;
            if (await _db.TransactionCodes.AnyAsync()) { _seeded = true; return; }
            var codes = new List<TransactionCodeEntity>();
            var sort = 0;
            foreach (var kvp in RouteMap)
            {
                var module = kvp.Value.Split('/')[1];
                var name = kvp.Key switch
                {
                    "MM01" => "Create Material", "MM02" => "Change Material", "MM03" => "Display Material",
                    "MMBE" => "Stock Overview", "ME11" => "Create Vendor", "ME12" => "Change Vendor",
                    "ME13" => "Display Vendor", "ME21N" => "Create Purchase Order",
                    "ME22N" => "Change Purchase Order", "ME23N" => "Display Purchase Order",
                    "MIGO" => "Goods Receipt", "MIRO" => "Invoice Verification",
                    "MB52" => "Stock List", "MB1A" => "Goods Issue", "MB1C" => "Goods Receipt (Other)",
                    "VA01" => "Create Sales Order", "VA02" => "Change Sales Order", "VA03" => "Display Sales Order",
                    "VA05" => "Sales Order List", "VLO1N" => "Create Delivery",
                    "VF01" => "Create Billing Document", "VD01" => "Create Customer",
                    "VD02" => "Change Customer", "VD03" => "Display Customer", "VKD1" => "Customer List",
                    "CS01" => "Create BOM", "CS02" => "Change BOM", "CS03" => "Display BOM",
                    "CO01" => "Create Production Order", "CO02" => "Change Production Order",
                    "CO03" => "Display Production Order", "MD01" => "MRP Run",
                    "MD04" => "Stock Requirements List", "CR01" => "Create Work Center",
                    "QE01" => "Create Inspection Lot", "QE02" => "Change Inspection Lot",
                    "QE03" => "Display Inspection Lot", "QS01" => "Create Inspection Plan",
                    "QS02" => "Change Inspection Plan", "QA01" => "Record Inspection Result",
                    "QUD" => "Usage Decision", "LT01" => "Create Transfer", "LT02" => "Change Transfer",
                    "LT03" => "Display Transfer", "LS01" => "Create Storage Location",
                    "LS02" => "Change Storage Location",
                    "FB50" => "Journal Entry", "FB60" => "AP Invoice", "FB70" => "AR Invoice",
                    "FBL1N" => "Vendor Line Items", "FBL5N" => "Customer Line Items",
                    "FS10N" => "G/L Account Balance", "FAGLL03" => "G/L Line Items",
                    "F.01" => "Balance Sheet", "F.02" => "Profit & Loss", "F-03" => "AP Payment",
                    "F-28" => "AR Payment", "KA01" => "Create Cost Center", "KA02" => "Change Cost Center",
                    "KA03" => "Display Cost Center", "KOB1" => "Cost Center Report",
                    "PA30" => "Maintain Employee", "PA20" => "Display Employee", "PA40" => "Employee List",
                    "PT60" => "Attendance Report", "PR01" => "Run Payroll", "PR05" => "Payroll History",
                    "CRM01" => "Create Lead", "CRM02" => "Edit Lead", "CRM03" => "Display Lead",
                    "CRM04" => "Create Opportunity", "CRM05" => "Edit Opportunity",
                    "CRM06" => "Pipeline Report", "LM01" => "Create Sample", "LM02" => "Edit Sample",
                    "LM03" => "Display Sample", "LM04" => "Record Test Result",
                    "LM05" => "Maintain Instrument", "BI01" => "Create Report", "BI02" => "Run Report",
                    "BI03" => "Display Dashboard", "BI04" => "Monitor KPI",
                    "SU01" => "User Management", "SU02" => "Tenant Management", "SU03" => "System Config",
                    "AL01" => "Audit Log", "AL02" => "Suspicious Activity",
                    "WF01" => "Workflow Designer", "WF02" => "Workflow Instances",
                    "AP01" => "Pending Approvals", "AP02" => "Approval History",
                    "NO01" => "Notifications", "TC01" => "Transaction Launcher",
                    "TC02" => "Transaction Management", "PL01" => "Plugin Management",
                    _ => kvp.Key
                };
                var group = kvp.Value.Split('/')[1] switch
                {
                    "MM" or "SD" or "PP" or "QM" or "WM" => "Transactions",
                    "FI" or "CO" => "Reports",
                    "HR" or "CRM" => "MasterData",
                    "LIMS" or "BI" => "Analytics",
                    "Admin" or "Audit" or "Workflow" or "Approval" or "Notifications" or "Transactions" or "Plugins" => "Administration",
                    _ => "Utilities"
                };
                codes.Add(new TransactionCodeEntity
                {
                    Code = kvp.Key,
                    Name = name,
                    Description = $"{name} ({kvp.Key})",
                    Module = module,
                    GroupName = group,
                    Route = kvp.Value,
                    Icon = GetIconForModule(module),
                    SortOrder = sort++,
                    Status = "Active",
                    IsSystem = true,
                    RequiredRole = GetRequiredRole(module)
                });
            }
            _db.TransactionCodes.AddRange(codes);
            await _db.SaveChangesAsync();
            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    private static string GetIconForModule(string module) => module switch
    {
        "MM" => "bi-boxes", "SD" => "bi-cart3", "PP" => "bi-gear", "QM" => "bi-clipboard-check",
        "WM" => "bi-house-door", "FI" => "bi-calculator", "CO" => "bi-pie-chart",
        "HR" => "bi-people", "CRM" => "bi-person-lines-fill", "LIMS" => "bi-flask",
        "BI" => "bi-graph-up", "Admin" => "bi-shield-lock", "Audit" => "bi-journal-text",
        "Workflow" => "bi-diagram-3", "Approval" => "bi-check2-square",
        "Notifications" => "bi-bell", "Transactions" => "bi-keyboard", "Plugins" => "bi-puzzle",
        _ => "bi-asterisk"
    };

    private static string GetRequiredRole(string module) => module switch
    {
        "Admin" or "Audit" => "ADMIN",
        "CO" or "FI" => "POWER_USER",
        _ => "NORMAL_USER"
    };
}
