using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesEdgeNode : MesEntityBase
{
    [MaxLength(64)]
    public string NodeCode { get; set; } = string.Empty;

    public DateTime? LastHeartbeat { get; set; }
    public int BufferDepth { get; set; }

    [MaxLength(32)]
    public string Version { get; set; } = "1.0.0";

    public double ClockOffsetMs { get; set; }
}

public class MesDataSource : MesEntityBase
{
    public Guid EdgeNodeId { get; set; }

    [MaxLength(32)]
    public string Protocol { get; set; } = "OPC_UA"; // OPC_UA, MQTT_SPARKPLUG_B, MODBUS_TCP, SIEMENS_S7, MTCONNECT

    [MaxLength(256)]
    public string Endpoint { get; set; } = string.Empty;

    [MaxLength(128)]
    public string CredentialsRef { get; set; } = string.Empty; // Secret store reference only

    [MaxLength(32)]
    public string Status { get; set; } = "ONLINE";
}

public class MesTag : MesEntityBase
{
    public Guid DataSourceId { get; set; }

    [MaxLength(128)]
    public string TagCode { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(32)]
    public string DataType { get; set; } = "DOUBLE";

    [MaxLength(32)]
    public string Uom { get; set; } = string.Empty;

    public int ScanClassMs { get; set; } = 1000;
    public double Deadband { get; set; }
    public Guid? ResourceId { get; set; }
    public TagSemanticRole SemanticRole { get; set; } = TagSemanticRole.TEMPERATURE;
    public double? MinValid { get; set; }
    public double? MaxValid { get; set; }
    public double? MaxRate { get; set; }
}

/// <summary>mes_tag_value_raw: partitioned daily in PostgreSQL.</summary>
public class MesTagValueRaw : MesEntityBase
{
    public Guid TagId { get; set; }
    public DateTime Ts { get; set; } = DateTime.UtcNow;
    public double? ValueNum { get; set; }

    [MaxLength(256)]
    public string? ValueText { get; set; }

    public TagQuality Quality { get; set; } = TagQuality.GOOD;
    public long Seq { get; set; }
    public Guid MessageUuid { get; set; } = Guid.NewGuid();
}

/// <summary>mes_tag_value_1m: warm 1-minute rollup for dashboards.</summary>
public class MesTagValue1m : MesEntityBase
{
    public Guid TagId { get; set; }
    public DateTime Bucket { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Avg { get; set; }
    public double Last { get; set; }
    public int Count { get; set; }
}

public class MesScanEvent : MesEntityBase
{
    public DateTime Ts { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string TerminalId { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    [MaxLength(256)]
    public string RawCode { get; set; } = string.Empty;

    [MaxLength(32)]
    public string CodeType { get; set; } = "BARCODE";

    [MaxLength(32)]
    public string? ResolvedEntityType { get; set; }

    public Guid? ResolvedEntityId { get; set; }
    public ScanResult Result { get; set; } = ScanResult.ACCEPTED;

    [MaxLength(256)]
    public string? RejectReason { get; set; }

    public Guid MessageUuid { get; set; } = Guid.NewGuid();
}

public class MesManualEntry : MesEntityBase
{
    [MaxLength(64)]
    public string FormCode { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    [MaxLength(128)]
    public string EnteredBy { get; set; } = "operator";

    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
}

public class MesDeadLetter : MesEntityBase
{
    public Guid MessageUuid { get; set; }

    [MaxLength(64)]
    public string Source { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    [MaxLength(512)]
    public string Error { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}
