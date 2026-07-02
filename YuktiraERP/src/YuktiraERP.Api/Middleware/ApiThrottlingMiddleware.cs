using System.Collections.Concurrent;

namespace YuktiraERP.Api.Middleware;

public class ApiThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequestsPerMinute;
    private readonly ConcurrentDictionary<string, ClientRequestTracker> _clients = new();

    public ApiThrottlingMiddleware(RequestDelegate next, int maxRequestsPerMinute = 100)
    {
        _next = next;
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tracker = _clients.GetOrAdd(clientIp, _ => new ClientRequestTracker());

        lock (tracker)
        {
            if (tracker.WindowStart == DateTime.MinValue || (DateTime.UtcNow - tracker.WindowStart).TotalMinutes >= 1)
            {
                tracker.WindowStart = DateTime.UtcNow;
                tracker.RequestCount = 0;
            }

            tracker.RequestCount++;

            var windowElapsed = (DateTime.UtcNow - tracker.WindowStart).TotalSeconds;
            var resetSeconds = (int)Math.Ceiling(Math.Max(0, 60 - windowElapsed));

            context.Response.Headers.TryAdd("X-RateLimit-Limit", _maxRequestsPerMinute.ToString());
            context.Response.Headers.TryAdd("X-RateLimit-Remaining", Math.Max(0, _maxRequestsPerMinute - tracker.RequestCount).ToString());
            context.Response.Headers.TryAdd("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(resetSeconds).ToUnixTimeSeconds().ToString());

            if (tracker.RequestCount > _maxRequestsPerMinute)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers.TryAdd("Retry-After", "60");
                return;
            }
        }

        await _next(context);
    }

    private class ClientRequestTracker
    {
        public DateTime WindowStart { get; set; } = DateTime.MinValue;
        public int RequestCount { get; set; }
    }
}
