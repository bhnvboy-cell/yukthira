using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class SapS4HanaConnector : IConnector
{
    private readonly HttpClient _http;
    public string ConnectorType => "SAP_S4HANA";
    public string Name => "SAP S/4HANA";
    public string Version => "1.0";
    public string Description => "Connects to SAP S/4HANA via OData v4 and REST APIs";
    public string[] SupportedAuthTypes => new[] { "Basic", "OAuth2", "SAP_Assertion" };
    public string[] SupportedActions => new[] { "GetMetadata", "ReadEntity", "CreateEntity", "UpdateEntity", "DeleteEntity", "CallFunction" };

    public SapS4HanaConnector(HttpClient http) => _http = http;

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            ApplyAuth(authType, authConfig);
            var url = $"{baseUrl.TrimEnd('/')}/$metadata";
            var resp = await _http.GetAsync(url);
            sw.Stop();
            return new() { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Connected" : $"HTTP {resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
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
            var url = $"{baseUrl.TrimEnd('/')}/{action}";
            var resp = parameters != null
                ? await _http.PostAsJsonAsync(url, parameters)
                : await _http.GetAsync(url);
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
        var url = $"{baseUrl.TrimEnd('/')}/{entityType}?$top=1000";
        if (lastSync.HasValue) url += $"&$filter=LastModifiedDateTime gt {lastSync:O}";
        var resp = await _http.GetFromJsonAsync<List<Dictionary<string, object>>>(url);
        return resp ?? new();
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        ApplyAuth(authType, authConfig);
        foreach (var rec in records)
        {
            var resp = await _http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/{entityType}", rec);
            if (!resp.IsSuccessStatusCode) return false;
        }
        return true;
    }

    private void ApplyAuth(string authType, Dictionary<string, string> cfg)
    {
        _http.DefaultRequestHeaders.Clear();
        if (authType == "Basic" && cfg.TryGetValue("Username", out var user) && cfg.TryGetValue("Password", out var pass))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")));
        else if (authType == "OAuth2" && cfg.TryGetValue("Token", out var token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
