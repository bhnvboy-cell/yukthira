using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YuktiraERP.Mes.EdgeGateway;

/// <summary>
/// Plant-floor L1 Edge Gateway worker (outbound-only to L2 per IEC 62443, Section 13.4).
/// </summary>
public class EdgeGatewayWorker : BackgroundService
{
    private readonly IEnumerable<IProtocolDriver> _drivers;
    private readonly EdgeStoreAndForwardBuffer _buffer;
    private readonly DeadbandFilter _deadband;
    private readonly ILogger<EdgeGatewayWorker> _logger;

    public EdgeGatewayWorker(
        IEnumerable<IProtocolDriver> drivers,
        EdgeStoreAndForwardBuffer buffer,
        DeadbandFilter deadband,
        ILogger<EdgeGatewayWorker> logger)
    {
        _drivers = drivers;
        _buffer = buffer;
        _deadband = deadband;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var driver in _drivers)
            {
                try
                {
                    var readings = await driver.PollAsync(stoppingToken);
                    foreach (var r in readings)
                    {
                        if (_deadband.ShouldEmit(r.TagId, r.ValueNum, 0.0))
                            _buffer.Enqueue(r);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[EdgeGateway] Poll error on driver {Protocol}", driver.Protocol);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
