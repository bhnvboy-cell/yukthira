namespace YuktiraERP.Core.Interfaces;

public class EmailMessage
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; }
    public string? TemplateCode { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
}

public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessage message, Guid tenantId);
}

public class SmsMessage
{
    public string To { get; set; } = "";
    public string Body { get; set; } = "";
    public string? TemplateCode { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
}

public interface ISmsSender
{
    Task<bool> SendAsync(SmsMessage message, Guid tenantId);
}