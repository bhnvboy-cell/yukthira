using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

/// <summary>mes_work_order: links to PP ProductionOrder by ProdOrderId.</summary>
public class MesWorkOrder : MesEntityBase
{
    [MaxLength(64)]
    public string WorkOrderNumber { get; set; } = string.Empty;

    public Guid ProdOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal PlannedQty { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.RELEASED;
    public int Priority { get; set; } = 100;
    public DateTime DueDate { get; set; }
}

/// <summary>mes_wo_operation</summary>
public class MesWoOperation : MesEntityBase
{
    public Guid WoId { get; set; }
    public int OperationNo { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ResourceId { get; set; }
    public WoOperationStatus Status { get; set; } = WoOperationStatus.QUEUED;
    public decimal QtyPlanned { get; set; }
    public decimal QtyGood { get; set; }
    public decimal QtyScrap { get; set; }
    public decimal QtyRework { get; set; }
    public double StdSetupMin { get; set; }
    public double StdRunMin { get; set; }
    public decimal OverlapPct { get; set; }
    public decimal TransferBatchQty { get; set; }
}

/// <summary>mes_dispatch_queue</summary>
public class MesDispatchQueue : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public int Seq { get; set; }
    public Guid WoOpId { get; set; }
    public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string? AcceptedBy { get; set; }

    public DateTime? AcceptedAt { get; set; }

    /// <summary>JSON flags: material, tool, doc, skill.</summary>
    public string ReadinessFlagsJson { get; set; } = "{\"material\":true,\"tool\":true,\"doc\":true,\"skill\":true}";
}

/// <summary>mes_dispatch_event (append-only).</summary>
public class MesDispatchEvent : MesEntityBase, IMesAuditAppendOnly
{
    public Guid WoOpId { get; set; }
    public DispatchEventType EventType { get; set; }
    public Guid? FromResourceId { get; set; }
    public Guid? ToResourceId { get; set; }

    [MaxLength(256)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ByUser { get; set; } = "system";

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}

/// <summary>mes_op_confirmation: idempotent floor confirmation with async ERP posting status.</summary>
public class MesOpConfirmation : MesEntityBase
{
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public Guid WoOpId { get; set; }
    public decimal GoodQty { get; set; }
    public decimal ScrapQty { get; set; }
    public decimal ReworkQty { get; set; }

    [MaxLength(64)]
    public string? ScrapReason { get; set; }

    public double LaborMin { get; set; }
    public double MachineMin { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    public PostingStatus PostingStatus { get; set; } = PostingStatus.PENDING;

    [MaxLength(64)]
    public string? ErpDocNo { get; set; }

    [MaxLength(512)]
    public string? ErrorText { get; set; }
}
