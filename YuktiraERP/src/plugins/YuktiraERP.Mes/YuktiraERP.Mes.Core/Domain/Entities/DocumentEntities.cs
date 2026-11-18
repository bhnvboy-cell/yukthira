using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesDocument : MesEntityBase
{
    [MaxLength(64)]
    public string DocNo { get; set; } = string.Empty;

    public MesDocType DocType { get; set; } = MesDocType.WORK_INSTRUCTION;

    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    [MaxLength(64)]
    public string RetentionClass { get; set; } = "STANDARD";
}

public class MesDocumentRevision : MesEntityBase, IMesAuditAppendOnly
{
    public Guid DocId { get; set; }

    [MaxLength(32)]
    public string Revision { get; set; } = "A";

    public DocRevisionStatus Status { get; set; } = DocRevisionStatus.DRAFT;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    [MaxLength(512)]
    public string StorageUri { get; set; } = string.Empty;

    public Guid? WorkflowInstanceId { get; set; }

    [MaxLength(64)]
    public string? ChangeNoticeNo { get; set; }

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}

public class MesDocumentLink : MesEntityBase
{
    public Guid RevId { get; set; }
    public DocLinkType LinkType { get; set; }
    public Guid LinkId { get; set; }
    public bool MandatoryAck { get; set; }
}

public class MesDocAcknowledgment : MesEntityBase
{
    public Guid UserId { get; set; }
    public Guid RevId { get; set; }
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? TrainingRef { get; set; }
}

public class MesEbrInstance : MesEntityBase
{
    public Guid? WoId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid TemplateRevId { get; set; }
    public EbrStatus Status { get; set; } = EbrStatus.OPEN;
    public DateTime? LockedAt { get; set; }
}

public class MesEbrStepEntry : MesEntityBase
{
    public Guid EbrId { get; set; }
    public int StepNo { get; set; }

    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Uom { get; set; } = string.Empty;

    public decimal? LowLimit { get; set; }
    public decimal? HighLimit { get; set; }
    public bool DeviationFlag { get; set; }

    [MaxLength(128)]
    public string EnteredBy { get; set; } = string.Empty;

    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public Guid? EsignId { get; set; }
}

/// <summary>mes_esignature: append-only, 21 CFR Part 11 and Annex 11 compatible.</summary>
public class MesESignature : MesEntityBase, IMesAuditAppendOnly
{
    public Guid UserId { get; set; }
    public ESignatureMeaning Meaning { get; set; } = ESignatureMeaning.APPROVED;
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string AuthMethod { get; set; } = "PASSWORD_MFA";

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}
