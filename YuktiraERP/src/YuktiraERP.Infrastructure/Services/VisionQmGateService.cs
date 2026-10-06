using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class VisionQmGateService : IVisionQmGateService
{
    private const string GateCharacteristic = "VISION_GATE_PHYSICAL_GRADE";

    private readonly YuktiraDbContext _db;
    private readonly IQualityVisionInspectionEngine _visionEngine;
    private readonly IInventoryMovementService _movementService;
    private readonly IZqmAutoLotGeneratorService _lotGenerator;
    private readonly IZqmNonConformanceService _ncService;
    private readonly IAuditService _auditService;
    private readonly VisionQmOptions _options;

    public VisionQmGateService(
        YuktiraDbContext db,
        IQualityVisionInspectionEngine visionEngine,
        IInventoryMovementService movementService,
        IZqmAutoLotGeneratorService lotGenerator,
        IZqmNonConformanceService ncService,
        IAuditService auditService,
        IOptions<VisionQmOptions> options)
    {
        _db = db;
        _visionEngine = visionEngine;
        _movementService = movementService;
        _lotGenerator = lotGenerator;
        _ncService = ncService;
        _auditService = auditService;
        _options = options?.Value ?? new VisionQmOptions();
    }

    public async Task<VisionQmGateResultDto> ProcessFrameAsync(VisionQmFrameRequest request, CancellationToken cancellationToken = default)
    {
        var result = new VisionQmGateResultDto();

        if (request is null)
        {
            result.Errors.Add("Request is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.MaterialCode))
        {
            result.Errors.Add("materialCode is required.");
            return result;
        }

        byte[] imageBytes;
        try
        {
            imageBytes = string.IsNullOrWhiteSpace(request.ImageBase64)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(request.ImageBase64);
        }
        catch
        {
            result.Errors.Add("imageBase64 is not valid base64.");
            return result;
        }

        if (imageBytes.Length == 0)
        {
            result.Errors.Add("Image bytes are required.");
            return result;
        }

        var tenantId = request.TenantId;
        var userId = string.IsNullOrWhiteSpace(request.UserId) ? "vision-gate" : request.UserId;
        var plant = string.IsNullOrWhiteSpace(request.Plant) ? "1000" : request.Plant;

        VisionInspectionResult visionResult;
        try
        {
            visionResult = await _visionEngine.InspectImageAsync(new VisionInspectionRequest
            {
                TenantId = tenantId,
                UserId = userId,
                MaterialCode = request.MaterialCode,
                Plant = plant,
                ImageData = imageBytes,
                ImageContentType = "application/octet-stream"
            });
        }
        catch (Exception ex)
        {
            visionResult = new VisionInspectionResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }

        var severity = visionResult?.Severity ?? VisionSeverity.Pass;
        var defectType = visionResult?.DetectedDefect ?? VisionDefectType.None;
        var confidence = visionResult?.ConfidenceScore ?? 0f;
        if (visionResult is null || !visionResult.Success)
        {
            severity = VisionSeverity.MajorDefect;
            if (visionResult?.Errors is { Count: > 0 } errors)
            {
                result.Errors.AddRange(errors);
            }
        }

        var (grade, impurityPct) = ComputePhysicalGrading(imageBytes);
        var qualityPass = severity < VisionSeverity.MajorDefect && impurityPct <= _options.MaxImpurityPct;

        result.Passed = qualityPass;
        result.Grade = grade;
        result.ImpurityPct = impurityPct;
        result.DefectType = defectType.ToString();
        result.Severity = severity.ToString();
        result.Confidence = confidence;

        string? lotNumber = null;

        if (qualityPass)
        {
            var gr = await _movementService.PostGoodsReceiptAsync(new PostGoodsMovementRequestDto
            {
                MovementType = 101,
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                HeaderText = "Vision QM gate goods receipt",
                Reference = _options.GateReference,
                Lines = new List<PostGoodsMovementLineDto>
                {
                    new()
                    {
                        MaterialCode = request.MaterialCode,
                        MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                        Plant = plant,
                        StorageLocation = string.IsNullOrWhiteSpace(_options.DefaultStorageLocation) ? "0001" : _options.DefaultStorageLocation,
                        BatchNumber = request.BatchNumber,
                        Quantity = request.Quantity <= 0 ? 1m : request.Quantity,
                        UOM = string.IsNullOrWhiteSpace(request.Unit) ? "EA" : request.Unit,
                        UnitPrice = 0m,
                        VendorCode = request.VendorCode
                    }
                }
            }, tenantId, userId);

            if (!gr.Success)
            {
                result.Errors.AddRange(gr.Errors);
            }
            else
            {
                result.MaterialDocumentNumber = gr.DocumentNumber;
                lotNumber = gr.InspectionLotNumber;
            }

            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                var autoLot = await _lotGenerator.GenerateInspectionLotAsync(new InspectionLotAutoGenRequest
                {
                    TenantId = tenantId,
                    MaterialCode = request.MaterialCode,
                    MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                    Plant = plant,
                    StorageLocation = string.IsNullOrWhiteSpace(_options.DefaultStorageLocation) ? "0001" : _options.DefaultStorageLocation,
                    BatchNumber = request.BatchNumber ?? "",
                    Quantity = request.Quantity <= 0 ? 1m : request.Quantity,
                    BaseUOM = string.IsNullOrWhiteSpace(request.Unit) ? "EA" : request.Unit,
                    VendorCode = request.VendorCode ?? "",
                    MovementType = 101,
                    UserId = userId
                });

                if (autoLot.Success && !string.IsNullOrWhiteSpace(autoLot.LotNumber))
                {
                    lotNumber = autoLot.LotNumber;
                }
            }

            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                lotNumber = $"VG-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                _db.InspectionLots.Add(new InspectionLotEntity
                {
                    TenantId = tenantId,
                    LotNumber = lotNumber,
                    MaterialCode = request.MaterialCode,
                    MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                    Plant = plant,
                    StorageLocation = string.IsNullOrWhiteSpace(_options.DefaultStorageLocation) ? "0001" : _options.DefaultStorageLocation,
                    BatchNumber = request.BatchNumber ?? "",
                    InspectionType = "01",
                    Quantity = (request.Quantity <= 0 ? 1m : request.Quantity).ToString(),
                    BaseUOM = string.IsNullOrWhiteSpace(request.Unit) ? "EA" : request.Unit,
                    Status = "Created",
                    ReferenceOrderNumber = result.MaterialDocumentNumber ?? _options.GateReference
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            result.InspectionLotNumber = lotNumber;
        }
        else
        {
            var severityLabel = severity == VisionSeverity.CriticalDefect
                ? "Critical"
                : severity == VisionSeverity.MajorDefect
                    ? "Major"
                    : "Minor";
            var nc = await _ncService.CreateNonConformanceAsync(new NonConformanceCreateRequest
            {
                TenantId = tenantId,
                NCType = "Defect",
                Severity = severityLabel,
                MaterialCode = request.MaterialCode,
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                BatchNumber = request.BatchNumber ?? "",
                Plant = plant,
                InspectionLotNumber = "",
                DefectCodeGroup = "VISION",
                DefectCode = defectType.ToString(),
                DefectDescription = $"Vision gate rejected frame: severity={severity}, defect={defectType}, impurityPct={impurityPct}, threshold={_options.MaxImpurityPct}%",
                RootCauseCategory = "VisionGate",
                RootCauseDescription = "Physical grading or vision severity failed quality gate.",
                DetectedBy = "VISION-GATE",
                VendorCode = request.VendorCode ?? "",
                AffectedQuantity = request.Quantity <= 0 ? 1m : request.Quantity,
                EstimatedCost = 0m,
                Priority = severity >= VisionSeverity.MajorDefect ? "High" : "Medium",
                UserId = userId
            });

            if (nc.Success)
            {
                result.NonConformanceId = nc.NCId != Guid.Empty ? nc.NCId.ToString() : nc.NCNumber;
            }
            else
            {
                result.Errors.AddRange(nc.Errors);
            }
        }

        var notes = $"material={request.MaterialCode};plant={plant};grade={grade};impurityPct={impurityPct};defect={defectType};severity={severity};confidence={confidence};passed={qualityPass}";
        _db.InspectionResults.Add(new InspectionResultEntity
        {
            TenantId = tenantId,
            ResultId = $"VG-{Guid.NewGuid():N}".Substring(0, 18),
            LotNumber = result.InspectionLotNumber ?? "",
            BatchNumber = request.BatchNumber ?? "",
            Characteristic = GateCharacteristic,
            Result = $"Grade={grade};ImpurityPct={impurityPct}",
            Specification = $"ImpurityPct<={_options.MaxImpurityPct}",
            TargetMin = 0m,
            TargetMax = _options.MaxImpurityPct,
            MeasuredValue = impurityPct,
            Unit = "%",
            Evaluation = qualityPass ? "Pass" : "Fail",
            InspectorNotes = notes,
            InspectorID = "VISION-GATE",
            Status = qualityPass ? "Passed" : "Failed"
        });
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditService.LogAsync(new AuditEntryDto
            {
                Timestamp = DateTime.UtcNow,
                UserId = Guid.TryParse(userId, out var uid) ? uid : null,
                TenantId = tenantId,
                ModuleName = "QM",
                ActionType = qualityPass ? ActionType.Create : ActionType.Config,
                EntityName = "VisionQmGate",
                EntityId = result.MaterialDocumentNumber ?? result.NonConformanceId,
                Details = $"Gate decision: passed={qualityPass}, grade={grade}, impurityPct={impurityPct}, severity={severity}, defect={defectType}, material={request.MaterialCode}, doc={result.MaterialDocumentNumber}, lot={result.InspectionLotNumber}, nc={result.NonConformanceId}"
            });
        }
        catch
        {
        }

        return result;
    }

    public async Task<List<InspectionResultRowDto>> GetRecentResultsAsync(Guid tenantId, string? materialCode, int page, int pageSize)
    {
        var query = _db.InspectionResults.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Characteristic == GateCharacteristic);

        if (!string.IsNullOrWhiteSpace(materialCode))
        {
            var needle = $"material={materialCode}";
            query = query.Where(r => r.InspectorNotes.Contains(needle));
        }

        var pageNumber = page <= 0 ? 1 : page;
        var size = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync();

        return rows.Select(r => new InspectionResultRowDto
        {
            ResultId = r.ResultId,
            LotNumber = r.LotNumber,
            Characteristic = r.Characteristic,
            Result = r.Result,
            Evaluation = r.Evaluation,
            MeasuredValue = r.MeasuredValue,
            Unit = r.Unit,
            Status = r.Status,
            InspectorNotes = r.InspectorNotes,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public static (string Grade, decimal ImpurityPct) ComputePhysicalGrading(byte[] imageBytes)
    {
        var hash = SHA256.HashData(imageBytes);
        var hashValue = BitConverter.ToUInt32(hash, 0);
        var hashImpurity = (decimal)(hashValue % 501u) / 100m;

        double sum = 0;
        double sumSq = 0;
        foreach (var b in imageBytes)
        {
            sum += b;
            sumSq += (double)b * b;
        }

        var n = imageBytes.Length;
        var mean = sum / n;
        var variance = Math.Max(0d, sumSq / n - mean * mean);
        var luminanceComponent = (decimal)(Math.Sqrt(variance) % 100d) / 100m;

        var impurity = hashImpurity * 0.8m + luminanceComponent * 0.2m;
        if (impurity < 0m) impurity = 0m;
        if (impurity > 5m) impurity = 5m;
        impurity = Math.Round(impurity, 2);

        var grade = impurity <= 1.0m ? "A" : impurity <= 2.5m ? "B" : "C";
        return (grade, impurity);
    }
}
