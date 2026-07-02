using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class LimsConnector : IConnector
{
    private readonly HttpClient _http;
    public string ConnectorType => "LIMS";
    public string Name => "Laboratory Information Management System";
    public string Version => "1.0";
    public string Description => "Connects to LIMS instruments and systems for sample tracking, test results, and instrument data";
    public string[] SupportedAuthTypes => new[] { "Basic", "APIKey", "Certificate" };
    public string[] SupportedActions => new[] { "GetTestResults", "SubmitSample", "GetInstrumentStatus", "GetCalibrationData", "GetSpecifications" };

    public LimsConnector(HttpClient http) => _http = http;

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            ApplyAuth(authType, authConfig);
            var resp = await _http.GetAsync($"{baseUrl.TrimEnd('/')}/api/v1/health");
            sw.Stop();
            return new() { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "LIMS reachable" : $"HTTP {resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
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
                "GetTestResults" => "tests/results",
                "SubmitSample" => "samples",
                "GetInstrumentStatus" => "instruments/status",
                "GetCalibrationData" => "calibration",
                "GetSpecifications" => "specifications",
                _ => action
            };
            var url = $"{baseUrl.TrimEnd('/')}/api/v1/{endpoint}";
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
        var url = $"{baseUrl.TrimEnd('/')}/api/v1/{entityType.ToLower()}";
        if (lastSync.HasValue) url += $"?modifiedSince={lastSync:O}";
        var resp = await _http.GetFromJsonAsync<List<Dictionary<string, object>>>(url);
        return resp ?? new();
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        ApplyAuth(authType, authConfig);
        foreach (var rec in records)
        {
            var resp = await _http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/v1/{entityType.ToLower()}", rec);
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
            _http.DefaultRequestHeaders.Add("X-LIMS-API-Key", key);
    }
}
