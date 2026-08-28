using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IInspectionResultService
{
    Task<InspectionResultDetailEntity> RecordResultAsync(InspectionResultDetailEntity result, string userId);
    Task<InspectionResultDetailEntity> RecordDefectAsync(string lotNumber, string defectCodeGroup, string defectCode, string defectDescription, string defectCategory, int quantity, string reportType, string userId);
    Task<List<InspectionResultDetailEntity>> GetResultsByLotAsync(string lotNumber);
    Task<UsageDecisionDetailEntity> ConfirmCertificateAsync(string lotNumber, string plant, string origin, string status, string userId);
    Task<UsageDecisionDetailEntity> RecordUsageDecisionAsync(string lotNumber, string udCode, string udDescription, string stockProposal, string userId);
    Task<List<UsageDecisionDetailEntity>> GetAllUsageDecisionsAsync(string? status = null, int take = 50);
    Task<UsageDecisionDetailEntity?> GetUsageDecisionByLotAsync(string lotNumber);
}

public class InspectionResultService : IInspectionResultService
{
    private readonly YuktiraDbContext _db;

    public InspectionResultService(YuktiraDbContext db) => _db = db;

    public async Task<InspectionResultDetailEntity> RecordResultAsync(InspectionResultDetailEntity result, string userId)
    {
        result.RecordedBy = userId;
        result.RecordedAt = DateTime.UtcNow;
        result.ResultStatus = "RECORDED";
        result.CreatedAt = DateTime.UtcNow;
        _db.InspectionResultDetails.Add(result);
        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<InspectionResultDetailEntity> RecordDefectAsync(string lotNumber, string defectCodeGroup, string defectCode, string defectDescription, string defectCategory, int quantity, string reportType, string userId)
    {
        var existing = await _db.InspectionResultDetails.FirstOrDefaultAsync(r => r.LotNumber == lotNumber && r.ResultStatus == "RECORDED");
        if (existing != null)
        {
            existing.DefectCodeGroup = defectCodeGroup;
            existing.DefectCode = defectCode;
            existing.DefectDescription = defectDescription;
            existing.DefectCategory = defectCategory;
            existing.ReportType = reportType;
            existing.DefectiveQuantity = quantity;
            existing.ResultStatus = "DEFECT_RECORDED";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var defect = new InspectionResultDetailEntity
            {
                LotNumber = lotNumber,
                DefectCodeGroup = defectCodeGroup,
                DefectCode = defectCode,
                DefectDescription = defectDescription,
                DefectCategory = defectCategory,
                ReportType = reportType,
                DefectiveQuantity = quantity,
                ResultStatus = "DEFECT_RECORDED",
                RecordedBy = userId,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.InspectionResultDetails.Add(defect);
        }
        await _db.SaveChangesAsync();
        return existing ?? await _db.InspectionResultDetails.FirstOrDefaultAsync(r => r.LotNumber == lotNumber && r.ResultStatus == "DEFECT_RECORDED")!;
    }

    public async Task<List<InspectionResultDetailEntity>> GetResultsByLotAsync(string lotNumber) =>
        await _db.InspectionResultDetails.Where(r => r.LotNumber == lotNumber).OrderByDescending(r => r.RecordedAt).ToListAsync();

    public async Task<UsageDecisionDetailEntity> ConfirmCertificateAsync(string lotNumber, string plant, string origin, string status, string userId)
    {
        var ud = await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);
        if (ud == null)
        {
            ud = new UsageDecisionDetailEntity
            {
                LotNumber = lotNumber,
                Plant = plant,
                InspectionLotOrigin = origin,
                ResultRecordingStatus = status,
                CertificateReceived = "Yes",
                Status = "CERT_CONFIRMED",
                DecidedBy = userId,
                DecisionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.UsageDecisionDetails.Add(ud);
        }
        else
        {
            ud.CertificateReceived = "Yes";
            ud.ResultRecordingStatus = status;
            ud.Status = "CERT_CONFIRMED";
            ud.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return ud;
    }

    public async Task<UsageDecisionDetailEntity> RecordUsageDecisionAsync(string lotNumber, string udCode, string udDescription, string stockProposal, string userId)
    {
        var ud = await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);
        if (ud == null)
        {
            ud = new UsageDecisionDetailEntity
            {
                LotNumber = lotNumber,
                UDCode = udCode,
                UDDescription = udDescription,
                StockProposal = stockProposal,
                Status = "UD_RECORDED",
                DecidedBy = userId,
                DecisionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.UsageDecisionDetails.Add(ud);
        }
        else
        {
            ud.UDCode = udCode;
            ud.UDDescription = udDescription;
            ud.StockProposal = stockProposal;
            ud.Status = "UD_RECORDED";
            ud.DecisionDate = DateTime.UtcNow;
            ud.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return ud;
    }

    public async Task<List<UsageDecisionDetailEntity>> GetAllUsageDecisionsAsync(string? status = null, int take = 50)
    {
        var q = _db.UsageDecisionDetails.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(u => u.Status == status);
        return await q.OrderByDescending(u => u.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<UsageDecisionDetailEntity?> GetUsageDecisionByLotAsync(string lotNumber) =>
        await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);
}
