using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IConnector
{
    string ConnectorType { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string[] SupportedAuthTypes { get; }
    string[] SupportedActions { get; }

    Task<TestConnectionResult> TestConnectionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig);
    Task<ConnectorActionResponse> ExecuteActionAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string action, Dictionary<string, object>? parameters);
    Task<List<Dictionary<string, object>>> PullDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, DateTime? lastSync);
    Task<bool> PushDataAsync(string baseUrl, string authType, Dictionary<string, string> authConfig, Dictionary<string, string> additionalConfig, string entityType, List<Dictionary<string, object>> records);
}

public class ConnectorRegistry
{
    private readonly Dictionary<string, IConnector> _connectors = new();

    public void Register(IConnector connector)
    {
        _connectors[connector.ConnectorType] = connector;
    }

    public IConnector? Get(string type) => _connectors.GetValueOrDefault(type);
    public List<IConnector> GetAll() => _connectors.Values.ToList();
}
