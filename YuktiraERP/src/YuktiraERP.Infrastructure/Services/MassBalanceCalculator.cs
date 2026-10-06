using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class MassBalanceCalculator : IMassBalanceCalculator
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly MassBalanceOptions _options;

    public MassBalanceCalculator(YuktiraDbContext db, ITenantContext tenant, IOptions<MassBalanceOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _options = options?.Value ?? new MassBalanceOptions();
    }

    public MassBalanceResult Calculate(MassBalanceInput input)
    {
        return CalculateCore(input?.Streams, input?.ThresholdPct, Guid.Empty, "ADHOC");
    }

    public async Task<MassBalanceResult> CalculateAdHocAsync(MassBalanceInput input, string userId, CancellationToken ct)
    {
        var result = CalculateCore(input?.Streams, input?.ThresholdPct, Guid.Empty, "ADHOC");
        var entity = await PersistAsync(result, ct);
        return ToDto(entity);
    }

    public async Task<MassBalanceResultEntity> CalculateForOrderAsync(Guid productionOrderId, string userId, CancellationToken ct)
    {
        var order = await _db.ProductionOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == productionOrderId, ct);
        if (order == null)
            throw new InvalidOperationException($"Production order {productionOrderId} was not found.");

        var streams = await BuildOrderStreamsAsync(order, ct);
        var result = CalculateCore(streams, null, order.Id, order.OrderNumber);
        return await PersistAsync(result, ct);
    }

    async Task<MassBalanceResult> IMassBalanceCalculator.CalculateForOrderAsync(Guid productionOrderId, string userId, CancellationToken ct)
    {
        return ToDto(await CalculateForOrderAsync(productionOrderId, userId, ct));
    }

    private async Task<List<MassBalanceStream>> BuildOrderStreamsAsync(ProductionOrderEntity order, CancellationToken ct)
    {
        var streams = new List<MassBalanceStream>();

        var issuedItems = await _db.ProductionOrderItems.AsNoTracking()
            .Where(i => i.ProductionOrderId == order.Id && i.IssuedQty > 0)
            .OrderBy(i => i.MaterialName)
            .ToListAsync(ct);

        if (issuedItems.Count > 0)
        {
            foreach (var item in issuedItems)
            {
                streams.Add(new MassBalanceStream
                {
                    Name = item.MaterialName,
                    Kind = MassBalanceKinds.RawInput,
                    MassKg = item.IssuedQty
                });
            }
        }
        else
        {
            var components = await _db.BillOfMaterials.AsNoTracking()
                .Where(b => b.TenantId == order.TenantId && b.ProductName == order.ProductName && b.Status == "Active")
                .OrderBy(b => b.ComponentName)
                .ToListAsync(ct);
            foreach (var component in components)
            {
                streams.Add(new MassBalanceStream
                {
                    Name = component.ComponentName,
                    Kind = MassBalanceKinds.RawInput,
                    MassKg = order.Quantity * component.Quantity
                });
            }
        }

        if (streams.Count == 0)
        {
            streams.Add(new MassBalanceStream
            {
                Name = "Confirmed yield and scrap",
                Kind = MassBalanceKinds.RawInput,
                MassKg = order.YieldQty + order.ScrapQty
            });
        }

        streams.Add(new MassBalanceStream
        {
            Name = order.ProductName,
            Kind = MassBalanceKinds.PrimaryOutput,
            MassKg = order.YieldQty
        });

        return streams;
    }

    private MassBalanceResult CalculateCore(
        IList<MassBalanceStream>? streams,
        decimal? thresholdInput,
        Guid productionOrderId,
        string orderNumber)
    {
        var threshold = thresholdInput ?? _options.DefaultThresholdPct;
        var normalized = Normalize(streams);

        decimal rawInput = 0;
        decimal intermediate = 0;
        decimal waterInput = 0;
        decimal primaryOutput = 0;
        decimal coProduct = 0;
        decimal dryInput = 0;
        decimal dryOutput = 0;

        foreach (var s in normalized)
        {
            switch (s.Kind)
            {
                case MassBalanceKinds.Intermediate:
                    intermediate += s.Mass;
                    dryInput += s.Dry;
                    waterInput += s.Water;
                    break;
                case MassBalanceKinds.PrimaryOutput:
                    primaryOutput += s.Mass;
                    dryOutput += s.Dry;
                    break;
                case MassBalanceKinds.CoProduct:
                    coProduct += s.Mass;
                    dryOutput += s.Dry;
                    break;
                default:
                    rawInput += s.Mass;
                    dryInput += s.Dry;
                    waterInput += s.Water;
                    break;
            }
        }

        var totalInput = rawInput + intermediate + waterInput;
        var totalOutput = primaryOutput;
        var yieldLoss = totalInput - totalOutput - coProduct;
        var yieldLossPct = totalInput == 0 ? 0m : yieldLoss / totalInput * 100m;
        var physicalYieldPct = totalInput == 0 ? 0m : (totalOutput + coProduct) / totalInput * 100m;
        var dryLoss = dryInput - dryOutput;
        var dryYieldPct = dryInput == 0 ? 0m : dryOutput / dryInput * 100m;

        var roundedStatus = R4(yieldLossPct) > R4(threshold) ? "Flagged" : "WithinTolerance";

        var breakdown = new
        {
            streams = normalized.Select(s => new
            {
                name = s.Name,
                kind = s.Kind,
                massKg = R4(s.Mass),
                moisturePct = R4(s.Moisture),
                waterKg = R4(s.Water),
                drySubstanceKg = R4(s.Dry)
            }).ToList(),
            kindSubtotals = new
            {
                rawInputKg = R4(rawInput),
                intermediateKg = R4(intermediate),
                waterInputKg = R4(waterInput),
                primaryOutputKg = R4(primaryOutput),
                coProductKg = R4(coProduct)
            },
            physicalYieldPct = R4(physicalYieldPct),
            drySubstanceYieldPct = R4(dryYieldPct),
            drySubstanceInputKg = R4(dryInput),
            drySubstanceOutputKg = R4(dryOutput),
            drySubstanceLossKg = R4(dryLoss),
            yieldLossKg = R4(yieldLoss),
            yieldLossPct = R4(yieldLossPct),
            thresholdPct = R4(threshold),
            status = roundedStatus
        };

        return new MassBalanceResult
        {
            Id = Guid.Empty,
            TenantId = Guid.Empty,
            ProductionOrderId = productionOrderId,
            OrderNumber = orderNumber ?? "",
            TotalInputKg = R4(totalInput),
            TotalOutputKg = R4(totalOutput),
            TotalCoProductKg = R4(coProduct),
            YieldLossKg = R4(yieldLoss),
            YieldLossPct = R4(yieldLossPct),
            PhysicalYieldPct = R4(physicalYieldPct),
            DrySubstanceInputKg = R4(dryInput),
            DrySubstanceOutputKg = R4(dryOutput),
            DrySubstanceLossKg = R4(dryLoss),
            DrySubstanceYieldPct = R4(dryYieldPct),
            ThresholdPct = R4(threshold),
            Status = roundedStatus,
            BreakdownJson = JsonSerializer.Serialize(breakdown),
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static List<NormalizedStream> Normalize(IList<MassBalanceStream>? streams)
    {
        var normalized = new List<NormalizedStream>();
        if (streams == null) return normalized;

        foreach (var stream in streams)
        {
            if (stream == null) continue;

            var mass = stream.MassKg < 0 ? 0m : stream.MassKg;
            var moisture = stream.MoisturePct < 0 ? 0m : (stream.MoisturePct > 100 ? 100m : stream.MoisturePct);
            var water = stream.WaterKg < 0 ? 0m : stream.WaterKg;
            var kind = stream.Kind;
            if (kind != MassBalanceKinds.Intermediate &&
                kind != MassBalanceKinds.PrimaryOutput &&
                kind != MassBalanceKinds.CoProduct)
            {
                kind = MassBalanceKinds.RawInput;
            }

            normalized.Add(new NormalizedStream
            {
                Name = stream.Name ?? "",
                Kind = kind,
                Mass = mass,
                Moisture = moisture,
                Water = water,
                Dry = mass * (1m - moisture / 100m)
            });
        }

        return normalized;
    }

    private async Task<MassBalanceResultEntity> PersistAsync(MassBalanceResult result, CancellationToken ct)
    {
        var entity = new MassBalanceResultEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            ProductionOrderId = result.ProductionOrderId,
            OrderNumber = result.OrderNumber,
            TotalInputKg = result.TotalInputKg,
            TotalOutputKg = result.TotalOutputKg,
            TotalCoProductKg = result.TotalCoProductKg,
            YieldLossKg = result.YieldLossKg,
            YieldLossPct = result.YieldLossPct,
            DrySubstanceInputKg = result.DrySubstanceInputKg,
            DrySubstanceOutputKg = result.DrySubstanceOutputKg,
            DrySubstanceLossKg = result.DrySubstanceLossKg,
            ThresholdPct = result.ThresholdPct,
            Status = result.Status,
            BreakdownJson = result.BreakdownJson,
            CalculatedAt = DateTime.UtcNow
        };

        _db.MassBalanceResults.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    private static MassBalanceResult ToDto(MassBalanceResultEntity entity)
    {
        var totalInput = entity.TotalInputKg;
        var totalOutput = entity.TotalOutputKg;
        var coProduct = entity.TotalCoProductKg;
        var dryInput = entity.DrySubstanceInputKg;
        var dryOutput = entity.DrySubstanceOutputKg;

        return new MassBalanceResult
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            ProductionOrderId = entity.ProductionOrderId,
            OrderNumber = entity.OrderNumber,
            TotalInputKg = totalInput,
            TotalOutputKg = totalOutput,
            TotalCoProductKg = coProduct,
            YieldLossKg = entity.YieldLossKg,
            YieldLossPct = entity.YieldLossPct,
            PhysicalYieldPct = R4(totalInput == 0 ? 0m : (totalOutput + coProduct) / totalInput * 100m),
            DrySubstanceInputKg = dryInput,
            DrySubstanceOutputKg = R4(dryOutput),
            DrySubstanceLossKg = entity.DrySubstanceLossKg,
            DrySubstanceYieldPct = R4(dryInput == 0 ? 0m : dryOutput / dryInput * 100m),
            ThresholdPct = entity.ThresholdPct,
            Status = entity.Status,
            BreakdownJson = entity.BreakdownJson,
            CalculatedAt = entity.CalculatedAt
        };
    }

    private static decimal R4(decimal value) => Math.Round(value, 4);

    private sealed class NormalizedStream
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = MassBalanceKinds.RawInput;
        public decimal Mass { get; set; }
        public decimal Moisture { get; set; }
        public decimal Water { get; set; }
        public decimal Dry { get; set; }
    }
}
