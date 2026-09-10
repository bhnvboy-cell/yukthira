using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmNonConformanceService : IZqmNonConformanceService
{
    private readonly YuktiraDbContext _db;

    public ZqmNonConformanceService(YuktiraDbContext db) => _db = db;

    public async Task<NonConformanceResult> CreateNonConformanceAsync(NonConformanceCreateRequest request)
    {
        var result = new NonConformanceResult();

        var ncNumber = await GenerateNCNumberAsync();
        var nc = new NonConformanceEntity
        {
            NCNumber = ncNumber,
            NCType = request.NCType,
            Severity = request.Severity,
            Status = "Open",
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            BatchNumber = request.BatchNumber,
            Plant = request.Plant,
            InspectionLotNumber = request.InspectionLotNumber,
            DefectCodeGroup = request.DefectCodeGroup,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            RootCauseCategory = request.RootCauseCategory,
            RootCauseDescription = request.RootCauseDescription,
            DetectedBy = request.DetectedBy,
            DetectedAt = DateTime.UtcNow,
            VendorCode = request.VendorCode,
            AffectedQuantity = request.AffectedQuantity,
            EstimatedCost = request.EstimatedCost,
            Priority = request.Priority,
            AssignedTo = request.AssignedTo,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Severity == "Critical" || request.AffectedQuantity > 100)
        {
            nc.IsContainmentActive = true;
            nc.ContainmentAction = "Automatic containment block due to critical severity or high volume.";
            nc.ContainmentExpiry = DateTime.UtcNow.AddDays(7);
            nc.Status = "Contained";
        }

        _db.NonConformances.Add(nc);
        await _db.SaveChangesAsync();

        result.Success = true;
        result.NCNumber = ncNumber;
        result.NCId = nc.Id;
        result.Status = nc.Status;
        result.ContainmentTriggered = nc.IsContainmentActive;

        return result;
    }

    public async Task<CapaResult> CreateCapaAsync(CapaCreateRequest request)
    {
        var result = new CapaResult();

        var nc = await _db.NonConformances.FirstOrDefaultAsync(n => n.NCNumber == request.NonConformanceNumber);
        if (nc == null && !string.IsNullOrEmpty(request.NonConformanceNumber))
        {
            result.Errors.Add($"Non-conformance {request.NonConformanceNumber} not found.");
            return result;
        }

        var cpNumber = await GenerateCPNumberAsync();
        var capa = new CAPAEntity
        {
            CPNumber = cpNumber,
            CPType = request.CPType,
            Status = "Draft",
            NonConformanceId = nc?.Id.ToString() ?? "",
            NCNumber = request.NonConformanceNumber,
            Title = request.Title,
            Description = request.Description,
            RootCauseCategory = request.RootCauseCategory,
            RootCauseAnalysis = request.RootCauseAnalysis,
            CorrectiveAction = request.CorrectiveAction,
            PreventiveAction = request.PreventiveAction,
            ResponsiblePerson = request.ResponsiblePerson,
            PlannedCompletionDate = request.PlannedCompletionDate,
            VerificationMethod = request.VerificationMethod,
            CreatedAt = DateTime.UtcNow
        };

        _db.Capas.Add(capa);

        if (nc != null)
        {
            nc.Status = "CorrectiveAction";
            nc.UpdatedAt = DateTime.UtcNow;
            _db.NonConformances.Update(nc);
        }

        await _db.SaveChangesAsync();

        result.Success = true;
        result.CPNumber = cpNumber;
        result.CPId = capa.Id;
        result.Status = "Draft";

        return result;
    }

    public async Task<bool> ActivateContainmentAsync(Guid ncId, string action, DateTime? expiry, string userId)
    {
        var nc = await _db.NonConformances.FindAsync(ncId);
        if (nc == null) return false;

        nc.IsContainmentActive = true;
        nc.ContainmentAction = action;
        nc.ContainmentExpiry = expiry;
        nc.Status = "Contained";
        nc.UpdatedAt = DateTime.UtcNow;
        _db.NonConformances.Update(nc);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteCapaAsync(Guid cpId, string userId, string notes)
    {
        var capa = await _db.Capas.FindAsync(cpId);
        if (capa == null) return false;

        capa.Status = "Completed";
        capa.ActualCompletionDate = DateTime.UtcNow;
        capa.EffectivenessNotes = notes;
        capa.UpdatedAt = DateTime.UtcNow;
        _db.Capas.Update(capa);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyCapaAsync(Guid cpId, string userId, bool effective, string notes)
    {
        var capa = await _db.Capas.FindAsync(cpId);
        if (capa == null) return false;

        capa.VerifiedBy = userId;
        capa.VerifiedAt = DateTime.UtcNow;
        capa.EffectivenessConfirmed = effective;
        capa.EffectivenessNotes = notes;
        capa.Status = effective ? "Closed" : "InProgress";
        capa.UpdatedAt = DateTime.UtcNow;
        _db.Capas.Update(capa);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<NonConformanceDto>> GetNonConformancesAsync(Guid tenantId, string? status = null)
    {
        var query = _db.NonConformances.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(n => n.Status == status);
        return await query.OrderByDescending(n => n.CreatedAt).Select(n => new NonConformanceDto
        {
            Id = n.Id, NCNumber = n.NCNumber, NCType = n.NCType, Severity = n.Severity, Status = n.Status,
            MaterialCode = n.MaterialCode, MaterialName = n.MaterialName, BatchNumber = n.BatchNumber, Plant = n.Plant,
            DefectCode = n.DefectCode, DefectDescription = n.DefectDescription,
            RootCauseCategory = n.RootCauseCategory, DetectedBy = n.DetectedBy, DetectedAt = n.DetectedAt,
            AffectedQuantity = n.AffectedQuantity, EstimatedCost = n.EstimatedCost,
            IsContainmentActive = n.IsContainmentActive, Priority = n.Priority, AssignedTo = n.AssignedTo,
            CreatedAt = n.CreatedAt
        }).ToListAsync();
    }

    public async Task<List<CapaDto>> GetCapasAsync(Guid tenantId, string? status = null)
    {
        var query = _db.Capas.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        return await query.OrderByDescending(c => c.CreatedAt).Select(c => new CapaDto
        {
            Id = c.Id, CPNumber = c.CPNumber, CPType = c.CPType, Status = c.Status, NCNumber = c.NCNumber,
            Title = c.Title, Description = c.Description, CorrectiveAction = c.CorrectiveAction,
            PreventiveAction = c.PreventiveAction, ResponsiblePerson = c.ResponsiblePerson,
            PlannedCompletionDate = c.PlannedCompletionDate, ActualCompletionDate = c.ActualCompletionDate,
            EffectivenessConfirmed = c.EffectivenessConfirmed, CreatedAt = c.CreatedAt
        }).ToListAsync();
    }

    private async Task<string> GenerateNCNumberAsync()
    {
        var last = await _db.NonConformances
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => n.NCNumber)
            .FirstOrDefaultAsync();
        if (last != null && last.StartsWith("NC") && int.TryParse(last.AsSpan(2), out var num))
            return $"NC{(num + 1):D8}";
        return $"NC{DateTime.UtcNow:yyyyMMdd}{new Random().Next(10, 99)}";
    }

    private async Task<string> GenerateCPNumberAsync()
    {
        var last = await _db.Capas
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.CPNumber)
            .FirstOrDefaultAsync();
        if (last != null && last.StartsWith("CP") && int.TryParse(last.AsSpan(2), out var num))
            return $"CP{(num + 1):D8}";
        return $"CP{DateTime.UtcNow:yyyyMMdd}{new Random().Next(10, 99)}";
    }
}
