namespace YuktiraERP.Core.Dtos;

public class WebhookDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string EventType { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
