using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

/// <summary>
/// mes_outbox_event: written in the same DB transaction as the business change.
/// Drained by MesOutboxPublisherService to IntegrationQueue, MrpBusBridge, and SignalR.
/// </summary>
public class MesOutboxEvent : MesEntityBase
{
    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public OutboxStatus Status { get; set; } = OutboxStatus.PENDING;
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    [MaxLength(512)]
    public string? LastError { get; set; }
}

/// <summary>
/// Tracks async MES-to-ERP postings (PP confirmations, MM StockMovement, CO activity, PM notifications, HR Attendance).
/// </summary>
public class MesErpPostingLog : MesEntityBase
{
    [MaxLength(32)]
    public string TargetModule { get; set; } = "PP"; // PP, MM, CO, PM, HR, QM

    [MaxLength(64)]
    public string PostingType { get; set; } = string.Empty;

    public Guid SourceEntityId { get; set; }
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public PostingStatus PostingStatus { get; set; } = PostingStatus.PENDING;

    [MaxLength(64)]
    public string? ErpDocNo { get; set; }

    public int AttemptCount { get; set; }

    [MaxLength(512)]
    public string? LastError { get; set; }

    public bool IsBackDated { get; set; }
    public DateTime? PostedAt { get; set; }
}
