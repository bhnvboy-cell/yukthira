using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.BackgroundServices;

/// <summary>
/// Thread-safe, process-wide store for the exchange rate sync state.
/// Registered as a singleton; written by the sync service (job or API triggered),
/// read by ExchangeRateController (sync-status / POST sync guard).
/// </summary>
public sealed class ExchangeRateSyncStatusStore : IExchangeRateSyncStatusStore
{
    private readonly object _gate = new();
    private bool _isRunning;
    private DateTime? _lastRunAt;
    private ExchangeRateSyncResultDto? _lastResult;

    public bool IsRunning
    {
        get { lock (_gate) return _isRunning; }
    }

    public DateTime? LastRunAt
    {
        get { lock (_gate) return _lastRunAt; }
    }

    public ExchangeRateSyncResultDto? LastResult
    {
        get { lock (_gate) return _lastResult; }
    }

    public bool TryBeginRun()
    {
        lock (_gate)
        {
            if (_isRunning) return false;
            _isRunning = true;
            return true;
        }
    }

    public void EndRun(ExchangeRateSyncResultDto result)
    {
        lock (_gate)
        {
            _isRunning = false;
            _lastResult = result;
            _lastRunAt = result.FinishedAt == default ? DateTime.UtcNow : result.FinishedAt;
        }
    }
}
