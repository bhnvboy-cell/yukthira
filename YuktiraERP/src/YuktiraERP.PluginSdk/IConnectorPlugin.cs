namespace YuktiraERP.PluginSdk;

public interface IConnectorPlugin : IYuktiraPlugin
{
    string ConnectorType { get; }
    string[] SupportedAuthTypes { get; }
    string[] SupportedActions { get; }
    Task<ConnectorPluginResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig);
    Task<ConnectorPluginResult> ExecuteActionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string action, Dictionary<string, object>? parameters);
}

public class ConnectorPluginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, object>? Data { get; set; }
    public int ResponseTimeMs { get; set; }
}
