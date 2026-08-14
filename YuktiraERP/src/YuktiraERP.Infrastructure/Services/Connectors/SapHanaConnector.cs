using System.Data;
using System.Text.Json;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class SapHanaConnector : IConnector
{
    private readonly HttpClient _http;
    public string ConnectorType => "SAP_HANA";
    public string Name => "SAP HANA";
    public string Version => "1.0";
    public string Description => "Connects to SAP HANA database via SQL and XS Engine REST";
    public string[] SupportedAuthTypes => new[] { "Basic", "JDBC", "XS_App" };
    public string[] SupportedActions => new[] { "ExecuteQuery", "ExecuteProcedure", "GetTables", "GetViews", "XSEngineCall" };

    public SapHanaConnector(HttpClient http) => _http = http;

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return new() { Success = false, Message = "Base URL is required", ResponseTimeMs = 0 };

            var url = baseUrl.TrimEnd('/');
            if (authType == "Basic")
                ApplyBasicAuth(authConfig);
            else if (authType == "XS_App")
                ApplyXsAuth(authConfig);

            var resp = await _http.GetAsync(url);
            sw.Stop();
            return new() { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Connected" : $"HTTP {(int)resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new() { Success = false, Message = ex.Message, ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
    }

    public async Task<ConnectorActionResponse> ExecuteActionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string action, Dictionary<string, object>? parameters)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return new() { Success = false, Message = "Base URL is required" };

            if (authType == "Basic") ApplyBasicAuth(authConfig);
            else if (authType == "XS_App") ApplyXsAuth(authConfig);

            var url = $"{baseUrl.TrimEnd('/')}/{action}";
            if (action == "ExecuteQuery" && parameters?.TryGetValue("query", out var q) == true)
                url = $"{url}?query={Uri.EscapeDataString(q?.ToString() ?? "")}";

            var resp = await _http.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();
            return new()
            {
                Success = resp.IsSuccessStatusCode,
                Message = resp.IsSuccessStatusCode ? $"Action {action} completed" : $"HTTP {(int)resp.StatusCode}",
                Data = new Dictionary<string, object> { ["statusCode"] = (int)resp.StatusCode, ["response"] = body }
            };
        }
        catch (Exception ex)
        {
            return new() { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> PullDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSync)
    {
        try
        {
            if (authType == "Basic") ApplyBasicAuth(authConfig);
            else if (authType == "XS_App") ApplyXsAuth(authConfig);

            var url = $"{baseUrl.TrimEnd('/')}/{entityType}";
            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return new();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        try
        {
            if (authType == "Basic") ApplyBasicAuth(authConfig);
            else if (authType == "XS_App") ApplyXsAuth(authConfig);

            var json = JsonSerializer.Serialize(records);
            var resp = await _http.PostAsync($"{baseUrl.TrimEnd('/')}/{entityType}",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyBasicAuth(Dictionary<string, string> authConfig)
    {
        if (authConfig.TryGetValue("Username", out var user) && authConfig.TryGetValue("Password", out var pass))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}");
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }
    }

    private void ApplyXsAuth(Dictionary<string, string> authConfig)
    {
        if (authConfig.TryGetValue("ApiKey", out var key))
            _http.DefaultRequestHeaders.Add("X-HANA-XS-API-Key", key);
    }
}
