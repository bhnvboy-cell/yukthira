using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesLot : MesEntityBase
{
    [MaxLength(64)]
    public string LotNo { get; set; } = string.Empty;

    public Guid MaterialId { get; set; }

    [MaxLength(64)]
    public string? ErpBatchNo { get; set; }

    public decimal Qty { get; set; }

    [MaxLength(16)]
    public string Uom { get; set; } = "EA";

    public MesLotStatus Status { get; set; } = MesLotStatus.UNRESTRICTED;
    public LotOrigin Origin { get; set; } = LotOrigin.PRODUCED;

    [MaxLength(64)]
    public string? VendorLotNo { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

public class MesUnit : MesEntityBase
{
    [MaxLength(64)]
    public string SerialNo { get; set; } = string.Empty;

    public Guid MaterialId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? WoId { get; set; }
    public int? CurrentOp { get; set; }

    [MaxLength(64)]
    public string CurrentLocation { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Status { get; set; } = "IN_WIP";

    public DateTime? ProducedAt { get; set; }
}

public class MesContainer : MesEntityBase
{
    [MaxLength(64)]
    public string ContainerCode { get; set; } = string.Empty;

    [MaxLength(32)]
    public string ContainerType { get; set; } = "CARTON"; // PALLET, TOTE, CARTON

    public Guid? ParentContainerId { get; set; }
    public string ContentsJson { get; set; } = "[]";
}

public class MesGenealogyEdge : MesEntityBase
{
    [MaxLength(32)]
    public string ParentType { get; set; } = "LOT"; // LOT, UNIT, CONTAINER

    public Guid ParentId { get; set; }

    [MaxLength(32)]
    public string ChildType { get; set; } = "UNIT";

    public Guid ChildId { get; set; }
    public decimal Qty { get; set; }
    public GenealogyEdgeType EdgeType { get; set; } = GenealogyEdgeType.CONSUMED_IN;
    public Guid? WoOpId { get; set; }
    public Guid? ResourceId { get; set; }
    public DateTime Ts { get; set; } = DateTime.UtcNow;
}

public class MesGenealogyClosure : MesEntityBase
{
    public Guid AncestorId { get; set; }
    public Guid DescendantId { get; set; }
    public int Depth { get; set; }
}

public class MesAsBuilt : MesEntityBase, IMesAuditAppendOnly
{
    public Guid UnitId { get; set; }

    [MaxLength(32)]
    public string BomRevision { get; set; } = "1";

    [MaxLength(32)]
    public string RoutingRevision { get; set; } = "1";

    [MaxLength(32)]
    public string RecipeVersion { get; set; } = "1.0";

    public string DocRevisionsJson { get; set; } = "[]";
    public string ComponentsJson { get; set; } = "[]";
    public string MeasurementsJson { get; set; } = "[]";
    public string OperatorsJson { get; set; } = "[]";
    public string ResourcesJson { get; set; } = "[]";
    public DateTime? LockedAt { get; set; }

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}

public class MesUnitHistory : MesEntityBase
{
    public Guid UnitId { get; set; }

    [MaxLength(64)]
    public string EventName { get; set; } = string.Empty;

    public Guid? WoOpId { get; set; }
    public string DetailJson { get; set; } = "{}";
    public DateTime Ts { get; set; } = DateTime.UtcNow;
}
