using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class MesConnector : IConnector
{
    private readonly HttpClient _http;
    public string ConnectorType => "MES";
    public string Name => "Manufacturing Execution System";
    public string Version => "1.0";
    public string Description => "Connects to MES systems via REST API for production orders, work instructions, and equipment data";
    public string[] SupportedAuthTypes => new[] { "Basic", "APIKey", "OAuth2" };
    public string[] SupportedActions => new[] { "GetProductionOrders", "GetWorkInstructions", "ReportProduction", "GetEquipmentStatus", "GetQualityData" };

    public MesConnector(HttpClient http) => _http = http;

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            ApplyAuth(authType, authConfig);
            var resp = await _http.GetAsync($"{baseUrl.TrimEnd('/')}/api/health");
            sw.Stop();
            return new() { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "MES reachable" : $"HTTP {resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
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
            ApplyAuth(authType, authConfig);
            var endpoint = action switch
            {
                "GetProductionOrders" => "production/orders",
                "GetWorkInstructions" => "production/work-instructions",
                "ReportProduction" => "production/report",
                "GetEquipmentStatus" => "equipment/status",
                "GetQualityData" => "quality/data",
                _ => action
            };
            var url = $"{baseUrl.TrimEnd('/')}/api/{endpoint}";
            var resp = parameters != null ? await _http.PostAsJsonAsync(url, parameters) : await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
            return new() { Success = resp.IsSuccessStatusCode, Message = $"HTTP {resp.StatusCode}", Data = data };
        }
        catch (Exception ex)
        {
            return new() { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> PullDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSync)
    {
        ApplyAuth(authType, authConfig);
        var url = $"{baseUrl.TrimEnd('/')}/api/{entityType.ToLower()}";
        if (lastSync.HasValue) url += $"?updatedSince={lastSync:O}";
        var resp = await _http.GetFromJsonAsync<List<Dictionary<string, object>>>(url);
        return resp ?? new();
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        ApplyAuth(authType, authConfig);
        foreach (var rec in records)
        {
            var resp = await _http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/{entityType.ToLower()}", rec);
            if (!resp.IsSuccessStatusCode) return false;
        }
        return true;
    }

    private void ApplyAuth(string authType, Dictionary<string, string> cfg)
    {
        _http.DefaultRequestHeaders.Clear();
        if (authType == "Basic" && cfg.TryGetValue("Username", out var u) && cfg.TryGetValue("Password", out var p))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{u}:{p}")));
        else if (authType == "APIKey" && cfg.TryGetValue("ApiKey", out var key))
            _http.DefaultRequestHeaders.Add("X-API-Key", key);
        else if (authType == "OAuth2" && cfg.TryGetValue("Token", out var t))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
    }
}
