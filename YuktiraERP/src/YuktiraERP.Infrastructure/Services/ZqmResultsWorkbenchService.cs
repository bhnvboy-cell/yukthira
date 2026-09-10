using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmResultsWorkbenchService : IZqmResultsWorkbenchService
{
    private readonly YuktiraDbContext _db;

    public ZqmResultsWorkbenchService(YuktiraDbContext db) => _db = db;

    public async Task<WorkbenchLotInfo?> GetLotForRecordingAsync(string lotNumber, Guid tenantId)
    {
        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
        if (lot == null) return null;

        var results = await _db.InspectionResults
            .Where(r => r.LotNumber == lotNumber)
            .ToListAsync();

        var details = await _db.InspectionResultDetails
            .Where(d => d.LotNumber == lotNumber)
            .ToListAsync();

        var characteristics = results.Select(r => new CharacteristicInfo
        {
            Id = r.Id,
            Characteristic = r.Characteristic,
            MICType = decimal.TryParse(r.Result, out _) ? "Quantitative" : "Qualitative",
            TargetValue = r.TargetMin,
            LSL = r.TargetMin,
            USL = r.TargetMax,
            Unit = r.Unit,
            IsRecorded = r.Status != "Pending",
            MeasuredValue = r.MeasuredValue,
            Evaluation = r.Evaluation
        }).ToList();

        return new WorkbenchLotInfo
        {
            LotId = lot.Id,
            LotNumber = lot.LotNumber,
            MaterialCode = lot.MaterialCode,
            MaterialName = lot.MaterialName,
            Plant = lot.Plant,
            BatchNumber = lot.BatchNumber,
            InspectionType = lot.InspectionType,
            SampleSize = lot.SampleSize,
            ResultsRecorded = lot.Inspected,
            ResultsPending = lot.SampleSize - lot.Inspected,
            Status = lot.Status,
            Characteristics = characteristics
        };
    }

    public async Task<ResultRecordingResponse> RecordCharacteristicResultAsync(ResultRecordingRequest request)
    {
        var response = new ResultRecordingResponse();

        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == request.LotNumber);
        if (lot == null)
        {
            response.Errors.Add($"Inspection lot {request.LotNumber} not found.");
            return response;
        }

        var isWithinSpec = true;
        decimal deviation = 0;
        string evaluation = "Pass";

        if (request.LSL.HasValue && request.USL.HasValue)
        {
            isWithinSpec = request.MeasuredValue >= request.LSL.Value && request.MeasuredValue <= request.USL.Value;
            var midpoint = (request.LSL.Value + request.USL.Value) / 2;
            deviation = Math.Abs(request.MeasuredValue - midpoint);
            evaluation = isWithinSpec ? "Pass" : "Fail";
        }
        else if (request.LSL.HasValue)
        {
            isWithinSpec = request.MeasuredValue >= request.LSL.Value;
            deviation = Math.Abs(request.MeasuredValue - request.LSL.Value);
            evaluation = isWithinSpec ? "Pass" : "Fail";
        }

        var deviationPercent = request.TargetValue != 0
            ? (deviation / Math.Abs(request.TargetValue) * 100).ToString("F2") + "%"
            : "N/A";

        var existingResult = await _db.InspectionResults.FirstOrDefaultAsync(r =>
            r.LotNumber == request.LotNumber && r.Characteristic == request.Characteristic);

        if (existingResult != null)
        {
            existingResult.MeasuredValue = request.MeasuredValue;
            existingResult.Evaluation = evaluation;
            existingResult.Status = isWithinSpec ? "Passed" : "Failed";
            existingResult.Result = request.MeasuredValue.ToString("F4");
            existingResult.InspectorNotes = request.Notes;
            existingResult.InspectorID = request.InspectorId;
            _db.InspectionResults.Update(existingResult);
        }
        else
        {
            var newResult = new InspectionResultEntity
            {
                LotNumber = request.LotNumber,
                Characteristic = request.Characteristic,
                TargetMin = request.LSL ?? 0,
                TargetMax = request.USL ?? 0,
                MeasuredValue = request.MeasuredValue,
                Unit = request.MeasuredUnit,
                Evaluation = evaluation,
                Status = isWithinSpec ? "Passed" : "Failed",
                Result = request.MeasuredValue.ToString("F4"),
                InspectorNotes = request.Notes,
                InspectorID = request.InspectorId,
                CreatedAt = DateTime.UtcNow
            };
            _db.InspectionResults.Add(newResult);
        }

        lot.Inspected++;
        if (isWithinSpec) lot.Passed++; else lot.Failed++;
        _db.InspectionLots.Update(lot);

        await _db.SaveChangesAsync();

        response.Success = true;
        response.Evaluation = evaluation;
        response.IsWithinSpec = isWithinSpec;
        response.Deviation = deviation;
        response.DeviationPercent = deviationPercent;
        response.ResultStatus = isWithinSpec ? "Passed" : "Failed";

        return response;
    }

    public async Task<List<CharacteristicInfo>> GetRecordedResultsAsync(string lotNumber)
    {
        return await _db.InspectionResults
            .Where(r => r.LotNumber == lotNumber)
            .Select(r => new CharacteristicInfo
            {
                Id = r.Id,
                Characteristic = r.Characteristic,
                TargetValue = r.TargetMin,
                LSL = r.TargetMin,
                USL = r.TargetMax,
                Unit = r.Unit,
                IsRecorded = r.Status != "Pending",
                MeasuredValue = r.MeasuredValue,
                Evaluation = r.Evaluation
            })
            .ToListAsync();
    }

    public async Task<bool> CompleteResultsRecordingAsync(string lotNumber, string userId)
    {
        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
        if (lot == null) return false;

        lot.Status = "ResultsRecorded";
        lot.UpdatedAt = DateTime.UtcNow;
        _db.InspectionLots.Update(lot);
        await _db.SaveChangesAsync();
        return true;
    }
}
