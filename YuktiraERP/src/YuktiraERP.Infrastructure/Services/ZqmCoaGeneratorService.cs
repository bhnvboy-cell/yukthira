using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmCoaGeneratorService : IZqmCoaGeneratorService
{
    private readonly YuktiraDbContext _db;

    public ZqmCoaGeneratorService(YuktiraDbContext db) => _db = db;

    public async Task<CoaGenerationResult> GenerateCoaAsync(CoaGenerationRequest request)
    {
        var result = new CoaGenerationResult();

        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == request.InspectionLotNumber);
        if (lot == null)
        {
            result.Errors.Add($"Inspection lot {request.InspectionLotNumber} not found.");
            return result;
        }

        var ud = await _db.UsageDecisions.FirstOrDefaultAsync(u => u.LotNumber == request.InspectionLotNumber);
        if (ud == null)
        {
            result.Errors.Add($"No usage decision found for lot {request.InspectionLotNumber}.");
            return result;
        }

        if (ud.UDCode != "Accepted" && ud.UDCode != "A")
        {
            result.Errors.Add($"Lot {request.InspectionLotNumber} was {ud.UDCode}. Only accepted lots get CoA.");
            return result;
        }

        var results = await _db.InspectionResults
            .Where(r => r.LotNumber == request.InspectionLotNumber)
            .ToListAsync();

        int total = results.Count;
        int passed = results.Count(r => r.Evaluation == "Pass" || r.Status == "Passed");
        int failed = total - passed;
        string overallResult = failed == 0 ? "PASS" : "FAIL";

        var coaNumber = await GenerateCoaNumberAsync();

        var cert = new CertificateOfAnalysisEntity
        {
            COANumber = coaNumber,
            InspectionLotNumber = request.InspectionLotNumber,
            MaterialCode = lot.MaterialCode,
            MaterialName = lot.MaterialName,
            BatchNumber = lot.BatchNumber,
            Plant = lot.Plant,
            IssuedBy = request.GeneratedBy,
            IssueDate = DateTime.UtcNow,
            CustomerName = request.CustomerName,
            OverallResult = overallResult,
            Status = "Issued",
            CreatedAt = DateTime.UtcNow
        };

        _db.CertificatesOfAnalysis.Add(cert);

        var log = new CoaGenerationLogEntity
        {
            CertificateNumber = coaNumber,
            InspectionLotNumber = request.InspectionLotNumber,
            MaterialCode = lot.MaterialCode,
            BatchNumber = lot.BatchNumber,
            CustomerCode = request.CustomerCode,
            CustomerName = request.CustomerName,
            TotalCharacteristics = total,
            PassedCharacteristics = passed,
            FailedCharacteristics = failed,
            OverallResult = overallResult,
            GeneratedBy = request.GeneratedBy,
            GeneratedAt = DateTime.UtcNow,
            Status = "Issued",
            DeliveryNoteNumber = request.DeliveryNoteNumber,
            MovementDocumentNumber = request.MovementDocumentNumber
        };

        _db.CoaGenerationLogs.Add(log);
        await _db.SaveChangesAsync();

        result.Success = true;
        result.CertificateNumber = coaNumber;
        result.TotalCharacteristics = total;
        result.PassedCharacteristics = passed;
        result.FailedCharacteristics = failed;
        result.OverallResult = overallResult;
        result.Status = "Issued";
        result.GeneratedAt = log.GeneratedAt;

        return result;
    }

    public async Task<List<CoaGenerationLogDto>> GetCoaLogsAsync(Guid tenantId, string? materialCode = null)
    {
        var query = _db.CoaGenerationLogs.AsQueryable();
        if (!string.IsNullOrEmpty(materialCode))
            query = query.Where(l => l.MaterialCode == materialCode);
        return await query.OrderByDescending(l => l.GeneratedAt).Select(l => new CoaGenerationLogDto
        {
            Id = l.Id, CertificateNumber = l.CertificateNumber, InspectionLotNumber = l.InspectionLotNumber,
            MaterialCode = l.MaterialCode, BatchNumber = l.BatchNumber, CustomerCode = l.CustomerCode,
            CustomerName = l.CustomerName, TotalCharacteristics = l.TotalCharacteristics,
            PassedCharacteristics = l.PassedCharacteristics, FailedCharacteristics = l.FailedCharacteristics,
            OverallResult = l.OverallResult, GeneratedBy = l.GeneratedBy, GeneratedAt = l.GeneratedAt,
            Status = l.Status
        }).ToListAsync();
    }

    private async Task<string> GenerateCoaNumberAsync()
    {
        var last = await _db.CertificatesOfAnalysis
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.COANumber)
            .FirstOrDefaultAsync();
        if (last != null && last.StartsWith("COA") && int.TryParse(last.AsSpan(3), out var num))
            return $"COA{(num + 1):D8}";
        return $"COA{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100, 999)}";
    }
}
