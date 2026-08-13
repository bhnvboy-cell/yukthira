using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var baseUrl = Arg("--base", "http://localhost:5000");
var userCount = int.Parse(Arg("--users", "100"));
var clientNumber = Arg("--client", "1000");
var includeDashboard = !HasFlag("--login-only");

Console.WriteLine($"Yuktira ERP Scalability Test");
Console.WriteLine($"  base      : {baseUrl}");
Console.WriteLine($"  users     : {userCount}");
Console.WriteLine($"  scenarios : {(includeDashboard ? "login + profile + dashboard" : "login only")}");
Console.WriteLine();

var results = new ConcurrentBag<Result>();
var errors = new ConcurrentBag<(int User, string Stage, int Status, string Detail)>();
var failedLogins = 0;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var wall = Stopwatch.StartNew();
using var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = 512,
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
};
using var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

var users = Enumerable.Range(1, userCount);
var tasks = users.Select(async i => await SimulateNodeAsync(i)).ToArray();
await Task.WhenAll(tasks);
wall.Stop();

var all = results.ToList();
var succeeded = all.Where(r => r.IsSuccess).ToList();
var failed = all.Where(r => !r.IsSuccess).ToList();
var durations = succeeded.Select(r => r.ElapsedMs).OrderBy(x => x).ToList();

Console.WriteLine();
Console.WriteLine("================= RESULTS =================");
Console.WriteLine($"Wall time          : {wall.Elapsed.TotalSeconds:F2}s");
Console.WriteLine($"Total requests     : {all.Count}");
Console.WriteLine($"Succeeded          : {succeeded.Count} ({(succeeded.Count * 100.0 / Math.Max(1, all.Count)):F1}%)");
Console.WriteLine($"Failed             : {failed.Count}");
Console.WriteLine($"Failed logins      : {failedLogins}");
Console.WriteLine($"Throughput         : {all.Count / wall.Elapsed.TotalSeconds:F1} req/s");
Console.WriteLine();

if (durations.Count > 0)
{
    Console.WriteLine($"Latency (successful)");
    Console.WriteLine($"  avg   : {durations.Average():F0} ms");
    Console.WriteLine($"  p50   : {Percentile(durations, 50):F0} ms");
    Console.WriteLine($"  p75   : {Percentile(durations, 75):F0} ms");
    Console.WriteLine($"  p95   : {Percentile(durations, 95):F0} ms");
    Console.WriteLine($"  p99   : {Percentile(durations, 99):F0} ms");
    Console.WriteLine($"  max   : {durations[^1]:F0} ms");
}

var statusGroups = all.GroupBy(r => r.Status).OrderBy(g => g.Key).ToList();
if (statusGroups.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("HTTP status distribution");
    foreach (var g in statusGroups)
        Console.WriteLine($"  {g.Key,-4}: {g.Count()}");
}

if (errors.Any())
{
    Console.WriteLine();
    Console.WriteLine("Sample errors (up to 15):");
    foreach (var e in errors.Take(15))
        Console.WriteLine($"  user {e.User:D3} stage={e.Stage} status={e.Status} {e.Detail}");
}

Console.WriteLine();
Console.WriteLine(succeeded.Count == all.Count
    ? "RESULT: PASS - all nodes completed successfully"
    : $"RESULT: FAIL - {failed.Count} requests did not succeed");
Environment.ExitCode = succeeded.Count == all.Count ? 0 : 1;

return;

async Task SimulateNodeAsync(int i)
{
    var userName = $"tester{i:D3}";
    var password = $"Test@123-{i:D3}";

    // 1. Login
    var (loginStatus, loginMs, token) = await LoginAsync(userName, password);
    results.Add(new Result(i, "login", loginStatus, loginMs, loginStatus == 200));

    if (loginStatus != 200 || string.IsNullOrEmpty(token))
    {
        Interlocked.Increment(ref failedLogins);
        errors.Add((i, "login", loginStatus, "no token returned"));
        return;
    }

    // 2. User profile
    var (pStatus, pMs) = await GetAsync("/api/auth/user-profile", token);
    results.Add(new Result(i, "profile", pStatus, pMs, pStatus == 200));
    if (pStatus != 200) errors.Add((i, "profile", pStatus, ""));

    // 3. Dashboard
    if (includeDashboard)
    {
        var (dStatus, dMs) = await GetAsync("/api/dashboard", token);
        results.Add(new Result(i, "dashboard", dStatus, dMs, dStatus == 200));
        if (dStatus != 200) errors.Add((i, "dashboard", dStatus, ""));
    }
}

async Task<(int Status, long Ms, string? Token)> LoginAsync(string userId, string password)
{
    var body = JsonSerializer.Serialize(new
    {
        clientNumber,
        userId,
        password,
        language = "EN",
        system = "DEV"
    });

    var sw = Stopwatch.StartNew();
    using var content = new StringContent(body, Encoding.UTF8, "application/json");
    try
    {
        using var response = await client.PostAsync("/api/auth/login", content);
        sw.Stop();
        var text = await response.Content.ReadAsStringAsync();
        var token = "";
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("accessToken", out var t))
                token = t.GetString() ?? "";
        }
        return ((int)response.StatusCode, sw.ElapsedMilliseconds, token);
    }
    catch (Exception ex)
    {
        sw.Stop();
        errors.Add((0, "login", 0, ex.Message));
        return (0, sw.ElapsedMilliseconds, null);
    }
}

async Task<(int Status, long Ms)> GetAsync(string path, string token)
{
    var sw = Stopwatch.StartNew();
    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        sw.Stop();
        return ((int)response.StatusCode, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();
        errors.Add((0, path, 0, ex.Message));
        return (0, sw.ElapsedMilliseconds);
    }
}

static double Percentile(List<long> sorted, int pct)
{
    if (sorted.Count == 0) return 0;
    var idx = (int)Math.Ceiling(pct / 100.0 * sorted.Count) - 1;
    return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
}

static string Arg(string name, string defaultValue)
{
    var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == name) return args[i + 1];
    return defaultValue;
}

static bool HasFlag(string name)
{
    return Environment.GetCommandLineArgs().Skip(1).Contains(name);
}

record Result(int User, string Stage, int Status, long ElapsedMs, bool IsSuccess);
