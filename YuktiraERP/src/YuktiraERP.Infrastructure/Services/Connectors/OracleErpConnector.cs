using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class OracleErpConnector : IConnector
{
    private readonly HttpClient _http;
    private readonly ILogger<OracleErpConnector> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

    public string ConnectorType => "ORACLE_ERP";
    public string Name => "Oracle ERP Cloud";
    public string Version => "2.0";
    public string Description => "Connects to Oracle ERP Cloud via REST API with OAuth2 authentication and error handling";
    public string[] SupportedAuthTypes => new[] { "Basic", "OAuth2", "Oracle_SSO" };
    public string[] SupportedActions => new[] { "GetEntity", "CreateEntity", "UpdateEntity", "GetAttachment", "GetItems", "GetPurchaseRequisitions", "GetInvoices" };

    public OracleErpConnector(HttpClient http) : this(http, null!) { }

    public OracleErpConnector(HttpClient http, ILogger<OracleErpConnector> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await ApplyAuthAsync(authType, authConfig, baseUrl);
            var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/metadata?onlyData=true";
            var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
            sw.Stop();
            return new TestConnectionResult { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Oracle ERP Cloud reachable" : $"HTTP {resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "Oracle ERP connection test failed");
            return new TestConnectionResult { Success = false, Message = ex.Message, ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
    }

    public async Task<ConnectorActionResponse> ExecuteActionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string action, Dictionary<string, object>? parameters)
    {
        try
        {
            await ApplyAuthAsync(authType, authConfig, baseUrl);
            return action switch
            {
                "GetItems" => await GetItemsAsync(baseUrl, parameters),
                "GetPurchaseRequisitions" => await GetPurchaseRequisitionsAsync(baseUrl, parameters),
                "GetInvoices" => await GetInvoicesAsync(baseUrl, parameters),
                _ => await ExecuteGenericActionAsync(baseUrl, action, parameters)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Oracle ERP action '{Action}' failed", action);
            return new ConnectorActionResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> PullDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSync)
    {
        await ApplyAuthAsync(authType, authConfig, baseUrl);
        return await SyncDataAsync(baseUrl, authType, authConfig, additionalConfig, entityType, lastSync);
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        await ApplyAuthAsync(authType, authConfig, baseUrl);
        foreach (var rec in records)
        {
            var resp = await ExecuteWithRetryAsync(() => _http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/{entityType}", rec));
            if (!resp.IsSuccessStatusCode)
            {
                var errorBody = await resp.Content.ReadAsStringAsync();
                _logger?.LogWarning("Oracle ERP push to {EntityType} failed: {StatusCode} - {Body}", entityType, resp.StatusCode, errorBody);
                return false;
            }
        }
        return true;
    }

    public async Task<Dictionary<string, object>> GetSchemaAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType)
    {
        await ApplyAuthAsync(authType, authConfig, baseUrl);
        var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/{entityType}/metadata";
        try
        {
            var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
            var body = await resp.Content.ReadAsStringAsync();
            return new Dictionary<string, object>
            {
                ["entityType"] = entityType,
                ["metadata"] = body,
                ["success"] = resp.IsSuccessStatusCode
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get Oracle ERP schema for {EntityType}", entityType);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> SyncDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSyncTime, int batchSize = 1000)
    {
        await ApplyAuthAsync(authType, authConfig, baseUrl);
        var allRecords = new List<Dictionary<string, object>>();
        var offset = 0;

        while (true)
        {
            var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/{entityType}?limit={batchSize}&offset={offset}&onlyData=true";
            if (lastSyncTime.HasValue)
                url += $"&q=LastUpdateDate>{lastSyncTime.Value:yyyy-MM-dd}";

            try
            {
                var resp = await ExecuteWithRetryAsync(() => _http.GetFromJsonAsync<List<Dictionary<string, object>>>(url));
                if (resp == null || resp.Count == 0) break;
                allRecords.AddRange(resp);
                offset += batchSize;
                if (resp.Count < batchSize) break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Oracle ERP sync failed at offset={Offset}", offset);
                break;
            }
        }

        return allRecords;
    }

    private async Task<ConnectorActionResponse> GetItemsAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/items?onlyData=true";
        if (parameters?.TryGetValue("ItemNumber", out var itemNum) == true)
            url += $"&q=ItemNumber='{itemNum}'";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Items retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> GetPurchaseRequisitionsAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/purchaseRequisitions?onlyData=true&limit=100";
        if (parameters?.TryGetValue("RequisitionNumber", out var reqNum) == true)
            url += $"&q=RequisitionNumber='{reqNum}'";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Purchase Requisitions retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> GetInvoicesAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/invoices?onlyData=true&limit=100";
        if (parameters?.TryGetValue("InvoiceNumber", out var invNum) == true)
            url += $"&q=InvoiceNumber='{invNum}'";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Invoices retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> ExecuteGenericActionAsync(string baseUrl, string action, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/fscmRestApi/resources/latest/{action.ToLower()}";
        var resp = parameters != null
            ? await ExecuteWithRetryAsync(() => _http.PostAsJsonAsync(url, parameters))
            : await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = $"HTTP {resp.StatusCode}", Data = data };
    }

    private async Task ApplyAuthAsync(string authType, Dictionary<string, string> cfg, string baseUrl)
    {
        _http.DefaultRequestHeaders.Clear();
        switch (authType)
        {
            case "Basic" when cfg.TryGetValue("Username", out var u) && cfg.TryGetValue("Password", out var p):
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{u}:{p}")));
                break;
            case "OAuth2" when cfg.TryGetValue("ClientId", out var clientId) && cfg.TryGetValue("ClientSecret", out var clientSecret):
                try
                {
                    var tokenUrl = $"{baseUrl.TrimEnd('/')}/oauth2/v1/token";
                    var tokenRequest = new { grant_type = "client_credentials", client_id = clientId, client_secret = clientSecret };
                    var tokenResp = await _http.PostAsJsonAsync(tokenUrl, tokenRequest);
                    if (tokenResp.IsSuccessStatusCode)
                    {
                        var tokenBody = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                        if (tokenBody?.TryGetValue("access_token", out var accessToken) == true)
                            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken?.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Oracle ERP OAuth2 token exchange failed, falling back to stored token");
                    if (cfg.TryGetValue("Token", out var storedToken))
                        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", storedToken);
                }
                break;
            case "OAuth2" when cfg.TryGetValue("Token", out var token):
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
        }
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException) when (attempt < MaxRetries - 1)
            {
                await Task.Delay(RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)]);
            }
        }
    }
}
