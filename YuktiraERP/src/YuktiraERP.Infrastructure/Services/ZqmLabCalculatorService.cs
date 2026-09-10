using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmLabCalculatorService : IZqmLabCalculatorService
{
    private readonly YuktiraDbContext _db;

    public ZqmLabCalculatorService(YuktiraDbContext db) => _db = db;

    public async Task<LabCalculationResult> CalculateAsync(LabCalculationRequest request)
    {
        var result = new LabCalculationResult();

        decimal convertedValue;
        string outputUnit;
        string formula;

        switch (request.CalculationType.ToUpperInvariant())
        {
            case "MOISTURE_CONTENT":
                convertedValue = CalculateMoistureContent(request.RawValue, 0);
                outputUnit = "%";
                formula = "Moisture% = (Wet - Dry) / Wet × 100";
                break;
            case "DRY_SUBSTANCE":
                convertedValue = CalculateDrySubstance(request.RawValue);
                outputUnit = "%";
                formula = "Dry Substance% = 100 - Moisture%";
                break;
            case "STARCH_PURITY":
                convertedValue = CalculateStarchPurity(request.RawValue, 0);
                outputUnit = "%";
                formula = "Starch Purity% = (Starch / Dry Substance) × 100";
                break;
            case "GRAIN_DEFECT":
                convertedValue = CalculateGrainDefect(request.RawValue, 100);
                outputUnit = "%";
                formula = "Grain Defect% = (Defective / Total) × 100";
                break;
            case "BAUME_GRAVITY":
                convertedValue = CalculateBaumeGravity(request.RawValue);
                outputUnit = "°Bé";
                formula = "°Bé = 145 - (145 / SG)";
                break;
            case "DE_VALUE":
                convertedValue = CalculateDEValue(request.RawValue, 100);
                outputUnit = "DE";
                formula = "DE = (Reducing Sugar / Total Dry Solids) × 100";
                break;
            case "PH_VALUE":
                convertedValue = request.RawValue;
                outputUnit = "pH";
                formula = "pH = -log₁₀[H⁺]";
                break;
            case "VISCOSITY":
                convertedValue = request.RawValue;
                outputUnit = "cP";
                formula = "Viscosity (cP) = Direct measurement";
                break;
            case "ASH_CONTENT":
                convertedValue = request.RawValue;
                outputUnit = "%";
                formula = "Ash% = (Ash weight / Sample weight) × 100";
                break;
            case "PROTEIN_CONTENT":
                convertedValue = request.RawValue;
                outputUnit = "%";
                formula = "Protein% = Nitrogen × 6.25";
                break;
            default:
                result.Errors.Add($"Unknown calculation type: {request.CalculationType}");
                return result;
        }

        bool isWithinSpec = true;
        string evaluation = "Pass";
        decimal? deviationPercent = null;

        if (request.LSL != 0 || request.USL != 0)
        {
            isWithinSpec = convertedValue >= request.LSL && convertedValue <= request.USL;
            evaluation = isWithinSpec ? "Pass" : "Fail";
            var midpoint = (request.LSL + request.USL) / 2;
            if (midpoint != 0)
                deviationPercent = Math.Abs(convertedValue - midpoint) / Math.Abs(midpoint) * 100;
        }

        var analysisNumber = await GenerateAnalysisNumberAsync();

        var labAnalysis = new LabAnalysisEntity
        {
            AnalysisNumber = analysisNumber,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            BatchNumber = request.BatchNumber,
            Plant = request.Plant,
            InspectionLotNumber = request.InspectionLotNumber,
            CalculationType = request.CalculationType,
            RawValue = request.RawValue,
            InputUnit = request.InputUnit,
            ConvertedValue = convertedValue,
            OutputUnit = outputUnit,
            LSL = request.LSL,
            USL = request.USL,
            IsWithinSpec = isWithinSpec,
            Evaluation = evaluation,
            Formula = formula,
            AnalyzedBy = request.AnalyzedBy,
            AnalyzedAt = DateTime.UtcNow,
            Method = request.Method,
            InstrumentId = request.InstrumentId,
            Notes = request.Notes
        };

        _db.LabAnalyses.Add(labAnalysis);
        await _db.SaveChangesAsync();

        result.Success = true;
        result.AnalysisNumber = analysisNumber;
        result.ConvertedValue = convertedValue;
        result.OutputUnit = outputUnit;
        result.IsWithinSpec = isWithinSpec;
        result.Evaluation = evaluation;
        result.Formula = formula;
        result.DeviationPercent = deviationPercent;

        return result;
    }

    public async Task<List<LabAnalysisDto>> GetAnalysisByLotAsync(string lotNumber, Guid tenantId)
    {
        return await _db.LabAnalyses
            .Where(l => l.InspectionLotNumber == lotNumber)
            .OrderByDescending(l => l.AnalyzedAt)
            .Select(l => new LabAnalysisDto
            {
                Id = l.Id, AnalysisNumber = l.AnalysisNumber, MaterialCode = l.MaterialCode,
                BatchNumber = l.BatchNumber, InspectionLotNumber = l.InspectionLotNumber,
                CalculationType = l.CalculationType, RawValue = l.RawValue, InputUnit = l.InputUnit,
                ConvertedValue = l.ConvertedValue, OutputUnit = l.OutputUnit, LSL = l.LSL, USL = l.USL,
                IsWithinSpec = l.IsWithinSpec, Evaluation = l.Evaluation, Formula = l.Formula,
                AnalyzedBy = l.AnalyzedBy, AnalyzedAt = l.AnalyzedAt
            })
            .ToListAsync();
    }

    public decimal CalculateMoistureContent(decimal wetBasis, decimal dryBasis)
    {
        if (wetBasis == 0) return 0;
        return dryBasis > 0 ? (wetBasis - dryBasis) / wetBasis * 100 : wetBasis;
    }

    public decimal CalculateDrySubstance(decimal moisture) => 100 - moisture;

    public decimal CalculateStarchPurity(decimal starch, decimal drySubstance)
    {
        if (drySubstance == 0) return 0;
        return starch / drySubstance * 100;
    }

    public decimal CalculateGrainDefect(decimal defectiveWeight, decimal totalWeight)
    {
        if (totalWeight == 0) return 0;
        return defectiveWeight / totalWeight * 100;
    }

    public decimal CalculateBaumeGravity(decimal density)
    {
        if (density <= 0) return 0;
        return 145 - (145 / density);
    }

    public decimal CalculateDEValue(decimal reducingSugar, decimal totalDrySolids)
    {
        if (totalDrySolids == 0) return 0;
        return reducingSugar / totalDrySolids * 100;
    }

    private async Task<string> GenerateAnalysisNumberAsync()
    {
        var last = await _db.LabAnalyses
            .OrderByDescending(l => l.AnalyzedAt)
            .Select(l => l.AnalysisNumber)
            .FirstOrDefaultAsync();
        if (last != null && last.StartsWith("LAB") && int.TryParse(last.AsSpan(3), out var num))
            return $"LAB{(num + 1):D8}";
        return $"LAB{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100, 999)}";
    }
}
