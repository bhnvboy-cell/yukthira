using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesGenealogyService
{
    private readonly MesDbContext _db;

    public MesGenealogyService(MesDbContext db) => _db = db;

    public async Task<MesGenealogyEdge> RecordEdgeAsync(
        Guid tenantId, string plantCode, string parentType, Guid parentId,
        string childType, Guid childId, decimal qty, GenealogyEdgeType edgeType,
        Guid? woOpId = null, Guid? resourceId = null, CancellationToken ct = default)
    {
        var edge = new MesGenealogyEdge
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            ParentType = parentType,
            ParentId = parentId,
            ChildType = childType,
            ChildId = childId,
            Qty = qty,
            EdgeType = edgeType,
            WoOpId = woOpId,
            ResourceId = resourceId,
            Ts = DateTime.UtcNow
        };
        _db.GenealogyEdges.Add(edge);

        // Maintain closure table: direct parent -> child (depth 1) + all ancestors of parent -> child
        var existingClosures = await _db.GenealogyClosures
            .Where(c => c.TenantId == tenantId && c.DescendantId == parentId)
            .ToListAsync(ct);

        var newLinks = new List<(Guid Ancestor, Guid Descendant, int Depth)>
        {
            (parentId, childId, 1)
        };
        foreach (var anc in existingClosures)
            newLinks.Add((anc.AncestorId, childId, anc.Depth + 1));

        foreach (var link in newLinks)
        {
            var exists = await _db.GenealogyClosures.AnyAsync(c =>
                c.TenantId == tenantId && c.AncestorId == link.Ancestor && c.DescendantId == link.Descendant, ct);
            if (!exists)
            {
                _db.GenealogyClosures.Add(new MesGenealogyClosure
                {
                    TenantId = tenantId,
                    PlantCode = plantCode,
                    AncestorId = link.Ancestor,
                    DescendantId = link.Descendant,
                    Depth = link.Depth
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return edge;
    }

    public async Task<IReadOnlyList<Guid>> TraceForwardDescendantsAsync(Guid tenantId, Guid lotOrUnitId, CancellationToken ct = default)
    {
        return await _db.GenealogyClosures
            .Where(c => c.TenantId == tenantId && c.AncestorId == lotOrUnitId)
            .OrderBy(c => c.Depth)
            .Select(c => c.DescendantId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> TraceBackwardAncestorsAsync(Guid tenantId, Guid unitOrLotId, CancellationToken ct = default)
    {
        return await _db.GenealogyClosures
            .Where(c => c.TenantId == tenantId && c.DescendantId == unitOrLotId)
            .OrderBy(c => c.Depth)
            .Select(c => c.AncestorId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<MesAsBuilt> LockAsBuiltAsync(
        Guid tenantId, string plantCode, Guid unitId,
        string bomRevision, string routingRevision, string recipeVersion,
        string componentsJson, string measurementsJson, string operatorsJson, string resourcesJson,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var asBuilt = new MesAsBuilt
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            UnitId = unitId,
            BomRevision = bomRevision,
            RoutingRevision = routingRevision,
            RecipeVersion = recipeVersion,
            ComponentsJson = componentsJson,
            MeasurementsJson = measurementsJson,
            OperatorsJson = operatorsJson,
            ResourcesJson = resourcesJson,
            LockedAt = now
        };
        asBuilt.RecordHash = RecordHashHelper.ComputeSha256(
            asBuilt.Id, unitId, bomRevision, routingRevision, recipeVersion, componentsJson, measurementsJson, now);
        _db.AsBuilts.Add(asBuilt);
        await _db.SaveChangesAsync(ct);
        return asBuilt;
    }
}
