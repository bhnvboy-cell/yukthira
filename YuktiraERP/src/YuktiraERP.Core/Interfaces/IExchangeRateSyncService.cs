namespace YuktiraERP.Core.Interfaces;

/// <summary>
/// Options bound from the "ExchangeRateSync" configuration section (both hosts).
/// </summary>
public class ExchangeRateSyncOptions
{
    public const string SectionName = "ExchangeRateSync";

    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "ECB";
    public string BaseCurrency { get; set; } = "EUR";
    public int IntervalHours { get; set; } = 24;
    public int StartupDelaySeconds { get; set; } = 30;
    public string? ApiKey { get; set; }
}

/// <summary>
/// Result of one sync run (job triggered or manually triggered via the API).
/// </summary>
public class ExchangeRateSyncResultDto
{
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Provider { get; set; } = "";
    public string BaseCurrency { get; set; } = "";
    public int RatesFetched { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success { get; set; }
}

/// <summary>
/// A currency feed (ECB, OpenExchangeRates, ...).
/// Implementations must never throw: failures are reported through <see cref="LastError"/>
/// and an empty rate dictionary so the background job cannot crash the host.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Stable provider name used in configuration ("ECB", "OpenExchangeRates").</summary>
    string Name { get; }

    /// <summary>Error message of the most recent failed fetch, otherwise null.</summary>
    string? LastError { get; }

    /// <summary>
    /// Fetches the latest rates for <paramref name="baseCurrency"/> and returns
    /// currency code (upper case) -> rate (units of the currency per 1 base currency).
    /// The base currency itself is included with rate 1 when possible.
    /// </summary>
    Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default);
}

/// <summary>
/// Process-wide, thread-safe record of the last/current sync run.
/// Singleton: written by <see cref="IExchangeRateSyncService"/>, read by the API.
/// </summary>
public interface IExchangeRateSyncStatusStore
{
    bool IsRunning { get; }
    DateTime? LastRunAt { get; }
    ExchangeRateSyncResultDto? LastResult { get; }

    /// <summary>Atomically claims the "running" flag. Returns false when a run is already in progress.</summary>
    bool TryBeginRun();

    /// <summary>Releases the "running" flag and records the finished run.</summary>
    void EndRun(ExchangeRateSyncResultDto result);
}

public interface IExchangeRateSyncService
{
    /// <summary>
    /// Fetches rates from the configured provider and upserts base -> currency rows for every
    /// target tenant. Updates the shared status store. Never throws for expected failures.
    /// </summary>
    Task<ExchangeRateSyncResultDto> SyncAsync(CancellationToken ct = default);
}
