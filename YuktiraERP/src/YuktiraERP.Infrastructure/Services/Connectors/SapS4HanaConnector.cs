using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services.Connectors;

public class SapS4HanaConnector : IConnector
{
    private readonly HttpClient _http;
    private readonly ILogger<SapS4HanaConnector> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

    public string ConnectorType => "SAP_S4HANA";
    public string Name => "SAP S/4HANA";
    public string Version => "2.0";
    public string Description => "Connects to SAP S/4HANA via OData v4 and REST APIs with Bearer token auth and retry policy";
    public string[] SupportedAuthTypes => new[] { "Basic", "OAuth2", "BearerToken" };
    public string[] SupportedActions => new[] { "GetMetadata", "ReadEntity", "CreateEntity", "UpdateEntity", "DeleteEntity", "CallFunction", "GetMaterialMaster", "GetPurchaseOrders", "GetSalesOrders", "PostGoodsReceipt" };

    public SapS4HanaConnector(HttpClient http) : this(http, null!) { }

    public SapS4HanaConnector(HttpClient http, ILogger<SapS4HanaConnector> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            ApplyAuth(authType, authConfig);
            var url = $"{baseUrl.TrimEnd('/')}/$metadata";
            var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
            sw.Stop();
            return new TestConnectionResult { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "SAP S/4HANA connected" : $"HTTP {resp.StatusCode}", ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "SAP S/4HANA connection test failed");
            return new TestConnectionResult { Success = false, Message = ex.Message, ResponseTimeMs = (int)sw.ElapsedMilliseconds };
        }
    }

    public async Task<ConnectorActionResponse> ExecuteActionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string action, Dictionary<string, object>? parameters)
    {
        try
        {
            ApplyAuth(authType, authConfig);
            return action switch
            {
                "GetMaterialMaster" => await GetMaterialMasterAsync(baseUrl, parameters),
                "GetPurchaseOrders" => await GetPurchaseOrdersAsync(baseUrl, parameters),
                "GetSalesOrders" => await GetSalesOrdersAsync(baseUrl, parameters),
                "PostGoodsReceipt" => await PostGoodsReceiptAsync(baseUrl, parameters),
                _ => await ExecuteGenericActionAsync(baseUrl, action, parameters)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SAP S/4HANA action '{Action}' failed", action);
            return new ConnectorActionResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> PullDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSync)
    {
        ApplyAuth(authType, authConfig);
        return await SyncDataAsync(baseUrl, authType, authConfig, additionalConfig, entityType, lastSync);
    }

    public async Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records)
    {
        ApplyAuth(authType, authConfig);
        foreach (var rec in records)
        {
            var resp = await ExecuteWithRetryAsync(() => _http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/{entityType}", rec));
            if (!resp.IsSuccessStatusCode)
            {
                _logger?.LogWarning("SAP S/4HANA push to {EntityType} failed: {StatusCode}", entityType, resp.StatusCode);
                return false;
            }
        }
        return true;
    }

    public async Task<Dictionary<string, object>> GetSchemaAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType)
    {
        ApplyAuth(authType, authConfig);
        var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/{entityType}/$metadata";
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
            _logger?.LogError(ex, "Failed to get schema for {EntityType}", entityType);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    public async Task<List<Dictionary<string, object>>> SyncDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSyncTime, int batchSize = 1000)
    {
        ApplyAuth(authType, authConfig);
        var allRecords = new List<Dictionary<string, object>>();
        var skip = 0;

        while (true)
        {
            var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/{entityType}?$top={batchSize}&$skip={skip}";
            if (lastSyncTime.HasValue)
                url += $"&$filter=LastModifiedDateTime gt {lastSyncTime.Value:O}";

            try
            {
                var resp = await ExecuteWithRetryAsync(() => _http.GetFromJsonAsync<List<Dictionary<string, object>>>(url));
                if (resp == null || resp.Count == 0) break;
                allRecords.AddRange(resp);
                skip += batchSize;
                if (resp.Count < batchSize) break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SAP S/4HANA sync failed at skip={Skip}", skip);
                break;
            }
        }

        return allRecords;
    }

    private async Task<ConnectorActionResponse> GetMaterialMasterAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/API_MATERIAL_MASTER_SRV/A_Material";
        if (parameters?.TryGetValue("Material", out var mat) == true)
            url += $"('{mat}')";
        url += "?$format=json";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Material Master retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> GetPurchaseOrdersAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/API_PURCHASEORDER_PROCESS_SRV/A_PurchaseOrder?$format=json&$top=100";
        if (parameters?.TryGetValue("PoNumber", out var po) == true)
            url += $"&$filter=PurchaseOrder eq '{po}'";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Purchase Orders retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> GetSalesOrdersAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/API_SALESORDER_SRV/A_SalesOrder?$format=json&$top=100";
        if (parameters?.TryGetValue("SalesOrder", out var so) == true)
            url += $"&$filter=SalesOrder eq '{so}'";

        var resp = await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = "Sales Orders retrieved", Data = data };
    }

    private async Task<ConnectorActionResponse> PostGoodsReceiptAsync(string baseUrl, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/sap/opu/odata/sap/API_GOODSMOVEMENT_SRV/A_GoodsMovement";
        var resp = await ExecuteWithRetryAsync(() => _http.PostAsJsonAsync(url, parameters ?? new()));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Goods Receipt posted" : $"HTTP {resp.StatusCode}", Data = data };
    }

    private async Task<ConnectorActionResponse> ExecuteGenericActionAsync(string baseUrl, string action, Dictionary<string, object>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{action}";
        var resp = parameters != null
            ? await ExecuteWithRetryAsync(() => _http.PostAsJsonAsync(url, parameters))
            : await ExecuteWithRetryAsync(() => _http.GetAsync(url));
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        return new ConnectorActionResponse { Success = resp.IsSuccessStatusCode, Message = $"HTTP {resp.StatusCode}", Data = data };
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

    private void ApplyAuth(string authType, Dictionary<string, string> cfg)
    {
        _http.DefaultRequestHeaders.Clear();
        switch (authType)
        {
            case "Basic" when cfg.TryGetValue("Username", out var user) && cfg.TryGetValue("Password", out var pass):
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")));
                break;
            case "OAuth2" when cfg.TryGetValue("Token", out var token):
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
            case "BearerToken" when cfg.TryGetValue("Token", out var bearerToken):
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                break;
        }
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
