using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.EdgeGateway;

public record EdgeTagReading(
    Guid TagId,
    DateTime SourceTimestamp,
    double? ValueNum,
    string? ValueText,
    TagQuality Quality,
    long SequenceNo,
    Guid MessageUuid);

public interface IProtocolDriver
{
    string Protocol { get; }
    Task<IReadOnlyList<EdgeTagReading>> PollAsync(CancellationToken ct = default);
}

/// <summary>
/// Deadband / report-by-exception filter (Section 1.2 & 1.5).
/// Reduces telemetry volume by 80-95% before store-and-forward.
/// </summary>
public class DeadbandFilter
{
    private readonly Dictionary<Guid, double> _lastValues = new();

    public bool ShouldEmit(Guid tagId, double? newValue, double deadband)
    {
        if (!newValue.HasValue) return true;
        if (!_lastValues.TryGetValue(tagId, out var prev) || Math.Abs(newValue.Value - prev) >= deadband)
        {
            _lastValues[tagId] = newValue.Value;
            return true;
        }
        return false;
    }
}

public class OpcUaProtocolDriver : IProtocolDriver
{
    public string Protocol => "OPC_UA";
    public Task<IReadOnlyList<EdgeTagReading>> PollAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgeTagReading>>(Array.Empty<EdgeTagReading>());
}

public class ModbusTcpProtocolDriver : IProtocolDriver
{
    public string Protocol => "MODBUS_TCP";
    public Task<IReadOnlyList<EdgeTagReading>> PollAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgeTagReading>>(Array.Empty<EdgeTagReading>());
}

public class SiemensS7ProtocolDriver : IProtocolDriver
{
    public string Protocol => "SIEMENS_S7";
    public Task<IReadOnlyList<EdgeTagReading>> PollAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgeTagReading>>(Array.Empty<EdgeTagReading>());
}

public class MqttSparkplugBProtocolDriver : IProtocolDriver
{
    public string Protocol => "MQTT_SPARKPLUG_B";
    public Task<IReadOnlyList<EdgeTagReading>> PollAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgeTagReading>>(Array.Empty<EdgeTagReading>());
}
