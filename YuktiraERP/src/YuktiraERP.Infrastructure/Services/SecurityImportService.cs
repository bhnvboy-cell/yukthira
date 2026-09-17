using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class SecurityImportService : ISecurityImportService
{
    private readonly YuktiraDbContext _db;
    private readonly IAuditService _audit;

    public SecurityImportService(YuktiraDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<SecurityImportResultDto> ImportMasterRolesAsync(List<SecurityImportRowDto> rows, Guid tenantId, Guid userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new SecurityImportResultDto();
        var errors = new List<SecurityImportErrorDto>();
        var batchNumber = await GenerateBatchNumberAsync(tenantId);

        var batch = new SecurityImportBatchEntity
        {
            TenantId = tenantId,
            BatchNumber = batchNumber,
            FileName = "MasterRoleImport",
            TotalRows = rows.Count,
            Status = "Processing"
        };
        _db.SecurityImportBatches.Add(batch);
        await _db.SaveChangesAsync();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            try
            {
                if (string.IsNullOrWhiteSpace(row.MasterRole))
                {
                    errors.Add(new SecurityImportErrorDto { RowNumber = i + 1, Column = "MasterRole", ErrorCode = "REQUIRED", Message = "MasterRole is required" });
                    continue;
                }

                var existing = await _db.MasterRoles
                    .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.RoleId == row.MasterRole);

                if (existing == null)
                {
                    _db.MasterRoles.Add(new MasterRoleEntity
                    {
                        TenantId = tenantId,
                        RoleId = row.MasterRole,
                        RoleName = row.MasterRole,
                        Module = row.Module,
                        SubProcess = row.SubProcess,
                        Catalog = row.Catalog,
                        Space = row.Space,
                        Description = row.AppDescription
                    });
                    result.MasterRolesCreated++;
                }

                if (!string.IsNullOrWhiteSpace(row.AppsTcodes))
                {
                    var tcodes = row.AppsTcodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var tcode in tcodes)
                    {
                        _db.RoleTCodeAssignments.Add(new RoleTCodeAssignmentEntity
                        {
                            TenantId = tenantId,
                            RoleId = row.MasterRole,
                            RoleType = "Master",
                            TransactionCode = tcode,
                            AppDescription = row.AppDescription
                        });
                        result.TCodeAssignmentsCreated++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new SecurityImportErrorDto { RowNumber = i + 1, Column = "General", ErrorCode = "EXCEPTION", Message = ex.Message });
            }
        }

        await _db.SaveChangesAsync();

        batch.ImportedRows = rows.Count - errors.Count;
        batch.ErrorRows = errors.Count;
        batch.Status = errors.Count > 0 ? "CompletedWithErrors" : "Completed";
        batch.ImportedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        sw.Stop();
        result.BatchNumber = batchNumber;
        result.Errors = errors.Count;
        result.ErrorDetails = errors;
        result.ElapsedMs = sw.ElapsedMilliseconds;
        result.Success = errors.Count == 0;

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = userId,
            TenantId = tenantId,
            ModuleName = "SecurityImport",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Create,
            EntityName = "MasterRoleImport",
            EntityId = batchNumber,
            NewValue = $"Imported {result.MasterRolesCreated} master roles",
            Details = $"Batch: {batchNumber}, Errors: {errors.Count}"
        });

        return result;
    }

    public async Task<SecurityImportResultDto> ImportCompositeRolesAsync(List<CompositeRoleImportRowDto> rows, Guid tenantId, Guid userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new SecurityImportResultDto();
        var errors = new List<SecurityImportErrorDto>();
        var batchNumber = await GenerateBatchNumberAsync(tenantId);

        var batch = new SecurityImportBatchEntity
        {
            TenantId = tenantId,
            BatchNumber = batchNumber,
            FileName = "CompositeRoleImport",
            TotalRows = rows.Count,
            Status = "Processing"
        };
        _db.SecurityImportBatches.Add(batch);
        await _db.SaveChangesAsync();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            try
            {
                if (string.IsNullOrWhiteSpace(row.CompositeRole))
                {
                    errors.Add(new SecurityImportErrorDto { RowNumber = i + 1, Column = "CompositeRole", ErrorCode = "REQUIRED", Message = "CompositeRole is required" });
                    continue;
                }

                var existingComposite = await _db.CompositeRoles
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CompositeRoleId == row.CompositeRole);

                if (existingComposite == null)
                {
                    _db.CompositeRoles.Add(new CompositeRoleEntity
                    {
                        TenantId = tenantId,
                        CompositeRoleId = row.CompositeRole,
                        CompositeRoleName = row.CompositeRole,
                        Module = row.Module
                    });
                    result.CompositeRolesCreated++;
                }

                if (!string.IsNullOrWhiteSpace(row.DerivedRole))
                {
                    var existingDerived = await _db.DerivedRoles
                        .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DerivedRoleId == row.DerivedRole);

                    if (existingDerived == null)
                    {
                        _db.DerivedRoles.Add(new DerivedRoleEntity
                        {
                            TenantId = tenantId,
                            DerivedRoleId = row.DerivedRole,
                            DerivedRoleName = row.DerivedRole,
                            CompositeRoleId = row.CompositeRole,
                            MasterRoleId = row.MasterRole,
                            Module = row.Module
                        });
                        result.DerivedRolesCreated++;
                    }

                    var assignment = new RoleTCodeAssignmentEntity
                    {
                        TenantId = tenantId,
                        RoleId = row.DerivedRole,
                        RoleType = "Derived",
                        TransactionCode = row.MasterRole,
                        AppDescription = $"Derived from {row.MasterRole}"
                    };
                    _db.RoleTCodeAssignments.Add(assignment);
                    result.TCodeAssignmentsCreated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new SecurityImportErrorDto { RowNumber = i + 1, Column = "General", ErrorCode = "EXCEPTION", Message = ex.Message });
            }
        }

        await _db.SaveChangesAsync();

        batch.ImportedRows = rows.Count - errors.Count;
        batch.ErrorRows = errors.Count;
        batch.Status = errors.Count > 0 ? "CompletedWithErrors" : "Completed";
        batch.ImportedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        sw.Stop();
        result.BatchNumber = batchNumber;
        result.Errors = errors.Count;
        result.ErrorDetails = errors;
        result.ElapsedMs = sw.ElapsedMilliseconds;
        result.Success = errors.Count == 0;

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = userId,
            TenantId = tenantId,
            ModuleName = "SecurityImport",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Create,
            EntityName = "CompositeRoleImport",
            EntityId = batchNumber,
            NewValue = $"Imported {result.CompositeRolesCreated} composite, {result.DerivedRolesCreated} derived, {result.MasterRolesCreated} master roles",
            Details = $"Batch: {batchNumber}, Errors: {errors.Count}"
        });

        return result;
    }

    public async Task<SecurityImportResultDto> ImportFullRoleMatrixAsync(RoleMatrixImportRequest request, Guid tenantId, Guid userId)
    {
        var masterResult = await ImportMasterRolesAsync(request.MasterRoleRows, tenantId, userId);
        var compositeResult = await ImportCompositeRolesAsync(request.CompositeRoleRows, tenantId, userId);

        return new SecurityImportResultDto
        {
            Success = masterResult.Success && compositeResult.Success,
            MasterRolesCreated = masterResult.MasterRolesCreated + compositeResult.MasterRolesCreated,
            CompositeRolesCreated = compositeResult.CompositeRolesCreated,
            DerivedRolesCreated = compositeResult.DerivedRolesCreated,
            TCodeAssignmentsCreated = masterResult.TCodeAssignmentsCreated + compositeResult.TCodeAssignmentsCreated,
            Errors = masterResult.Errors + compositeResult.Errors,
            ErrorDetails = masterResult.ErrorDetails.Concat(compositeResult.ErrorDetails).ToList(),
            BatchNumber = $"{masterResult.BatchNumber},{compositeResult.BatchNumber}",
            ElapsedMs = masterResult.ElapsedMs + compositeResult.ElapsedMs
        };
    }

    public async Task<List<MasterRoleDto>> GetMasterRolesAsync(Guid tenantId, string? module = null)
    {
        var query = _db.MasterRoles.Where(m => m.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(m => m.Module == module);
        return await query.OrderBy(m => m.RoleId)
            .Select(m => new MasterRoleDto
            {
                Id = m.RoleId,
                Name = m.RoleName,
                Module = m.Module,
                Description = m.Description
            })
            .ToListAsync();
    }

    public async Task<List<CompositeRoleDto>> GetCompositeRolesAsync(Guid tenantId, string? module = null)
    {
        var query = _db.CompositeRoles.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(c => c.Module == module);
        return await query.OrderBy(c => c.CompositeRoleId)
            .Select(c => new CompositeRoleDto
            {
                Id = c.CompositeRoleId,
                Name = c.CompositeRoleName,
                Module = c.Module,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<List<DerivedRoleDto>> GetDerivedRolesAsync(string compositeRoleId)
    {
        return await _db.DerivedRoles
            .Where(d => d.CompositeRoleId == compositeRoleId)
            .OrderBy(d => d.DerivedRoleId)
            .Select(d => new DerivedRoleDto
            {
                DerivedRoleId = d.DerivedRoleId,
                DerivedRoleName = d.DerivedRoleName,
                MasterRoleId = d.MasterRoleId,
                TCodeAssignments = _db.RoleTCodeAssignments
                    .Where(a => a.RoleId == d.DerivedRoleId && a.RoleType == "Derived")
                    .Select(a => new TCodeAssignmentDto
                    {
                        TransactionCode = a.TransactionCode,
                        AppDescription = a.AppDescription,
                        HasAccess = a.HasAccess
                    }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<RoleHierarchyDto>> GetRoleHierarchyAsync(Guid tenantId)
    {
        var composites = await _db.CompositeRoles
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.CompositeRoleId)
            .ToListAsync();

        var derived = await _db.DerivedRoles
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.DerivedRoleId)
            .ToListAsync();

        var tcodeAssignments = await _db.RoleTCodeAssignments
            .Where(a => a.TenantId == tenantId)
            .ToListAsync();

        var hierarchy = new List<RoleHierarchyDto>();
        foreach (var comp in composites)
        {
            var derivedRoles = derived
                .Where(d => d.CompositeRoleId == comp.CompositeRoleId)
                .Select(d => new DerivedRoleDto
                {
                    DerivedRoleId = d.DerivedRoleId,
                    DerivedRoleName = d.DerivedRoleName,
                    MasterRoleId = d.MasterRoleId,
                    TCodeAssignments = tcodeAssignments
                        .Where(a => a.RoleId == d.DerivedRoleId && a.RoleType == "Derived")
                        .Select(a => new TCodeAssignmentDto
                        {
                            TransactionCode = a.TransactionCode,
                            AppDescription = a.AppDescription,
                            HasAccess = a.HasAccess
                        }).ToList()
                }).ToList();

            hierarchy.Add(new RoleHierarchyDto
            {
                CompositeRoleId = comp.CompositeRoleId,
                CompositeRoleName = comp.CompositeRoleName,
                DerivedRoles = derivedRoles
            });
        }

        return hierarchy;
    }

    public async Task<UserRoleAssignResultDto> AssignCompositeRoleToUserAsync(UserRoleAssignRequest request, Guid tenantId, string assignedByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.CompositeRoleId))
        {
            return new UserRoleAssignResultDto { Success = false, Message = "UserId and CompositeRoleId are required" };
        }

        var composite = await _db.CompositeRoles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CompositeRoleId == request.CompositeRoleId);

        if (composite == null)
        {
            return new UserRoleAssignResultDto { Success = false, Message = $"Composite role '{request.CompositeRoleId}' not found" };
        }

        var existing = await _db.UserRoleAssignments
            .FirstOrDefaultAsync(u => u.TenantId == tenantId
                && u.UserId == request.UserId
                && u.CompositeRoleId == request.CompositeRoleId
                && u.Status == "Active");

        if (existing != null)
        {
            return new UserRoleAssignResultDto { Success = false, Message = "User already assigned to this composite role" };
        }

        var assignment = new UserRoleAssignmentEntity
        {
            TenantId = tenantId,
            UserId = request.UserId,
            CompositeRoleId = request.CompositeRoleId,
            AssignedBy = assignedByUserId,
            AssignedAt = DateTime.UtcNow
        };
        _db.UserRoleAssignments.Add(assignment);

        var derivedRoles = await _db.DerivedRoles
            .Where(d => d.TenantId == tenantId && d.CompositeRoleId == request.CompositeRoleId)
            .ToListAsync();

        int permissionsGranted = 0;
        foreach (var derived in derivedRoles)
        {
            var derivedTcodes = await _db.RoleTCodeAssignments
                .Where(a => a.TenantId == tenantId && a.RoleId == derived.DerivedRoleId && a.RoleType == "Derived" && a.HasAccess)
                .ToListAsync();
            permissionsGranted += derivedTcodes.Count;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = Guid.TryParse(assignedByUserId, out var assignerId) ? assignerId : null,
            TenantId = tenantId,
            ModuleName = "SecurityImport",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Create,
            EntityName = "UserRoleAssignment",
            EntityId = request.UserId,
            NewValue = $"Assigned composite role {request.CompositeRoleId}, {permissionsGranted} permissions granted"
        });

        return new UserRoleAssignResultDto
        {
            Success = true,
            PermissionsGranted = permissionsGranted,
            Message = $"Role assigned successfully. {permissionsGranted} permissions granted."
        };
    }

    public async Task<List<UserRoleAssignmentDto>> GetUserRoleAssignmentsAsync(string userId, Guid tenantId)
    {
        return await _db.UserRoleAssignments
            .Where(u => u.TenantId == tenantId && u.UserId == userId)
            .OrderBy(u => u.CompositeRoleId)
            .Select(u => new UserRoleAssignmentDto
            {
                UserId = u.UserId,
                CompositeRoleId = u.CompositeRoleId,
                CompositeRoleName = u.CompositeRoleId,
                AssignedAt = u.AssignedAt
            })
            .ToListAsync();
    }

    public async Task<List<RoleTCodeAssignmentDto>> GetRoleTCodePermissionsAsync(string roleId, string roleType, Guid tenantId)
    {
        return await _db.RoleTCodeAssignments
            .Where(a => a.TenantId == tenantId && a.RoleId == roleId && a.RoleType == roleType)
            .Select(a => new RoleTCodeAssignmentDto
            {
                TransactionCode = a.TransactionCode,
                Description = a.AppDescription,
                Module = a.RoleType,
                HasAccess = a.HasAccess
            })
            .ToListAsync();
    }

    public async Task<int> GetTCodeCountAsync(Guid tenantId)
    {
        return await _db.RoleTCodeAssignments
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.TransactionCode)
            .Distinct()
            .CountAsync();
    }

    public async Task<int> GetRoleCountAsync(Guid tenantId)
    {
        var masterCount = await _db.MasterRoles.CountAsync(m => m.TenantId == tenantId);
        var compositeCount = await _db.CompositeRoles.CountAsync(c => c.TenantId == tenantId);
        var derivedCount = await _db.DerivedRoles.CountAsync(d => d.TenantId == tenantId);
        return masterCount + compositeCount + derivedCount;
    }

    private async Task<string> GenerateBatchNumberAsync(Guid tenantId)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"SEC-{datePart}-";
        var lastBatch = await _db.SecurityImportBatches
            .Where(b => b.TenantId == tenantId && b.BatchNumber.StartsWith(prefix))
            .OrderByDescending(b => b.BatchNumber)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (lastBatch != null)
        {
            var parts = lastBatch.BatchNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}{seq:D4}";
    }
}
