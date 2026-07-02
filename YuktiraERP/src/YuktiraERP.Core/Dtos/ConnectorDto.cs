namespace YuktiraERP.Core.Dtos;

public class ConnectorDto
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "";
    public string[] SupportedAuthTypes { get; set; } = Array.Empty<string>();
    public string[] SupportedActions { get; set; } = Array.Empty<string>();
}

public class IntegrationConnectionDto
{
    public Guid Id { get; set; }
    public string ConnectorType { get; set; } = "";
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string AuthType { get; set; } = "None";
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime? LastTestedAt { get; set; }
    public string LastTestResult { get; set; } = "";
}

public class MappingRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string SourceSystem { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public string SourceEntity { get; set; } = "";
    public string TargetEntity { get; set; } = "";
    public List<FieldMappingDto> FieldMappings { get; set; } = new();
    public string TransformationScript { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class FieldMappingDto
{
    public string SourceField { get; set; } = "";
    public string TargetField { get; set; } = "";
    public string DefaultValue { get; set; } = "";
    public string TransformExpression { get; set; } = "";
    public bool IsRequired { get; set; }
}

public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int ResponseTimeMs { get; set; }
}

public class ConnectorActionRequest
{
    public Guid ConnectionId { get; set; }
    public string Action { get; set; } = "";
    public Dictionary<string, object>? Parameters { get; set; }
}

public class ConnectorActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, object>? Data { get; set; }
}
