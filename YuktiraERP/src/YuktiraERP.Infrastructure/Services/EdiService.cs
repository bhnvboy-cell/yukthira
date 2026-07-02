using System.Text.Json;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class EdiService : IEdiService
{
    public Task<string> ConvertToEdifactAsync(object data, string documentType)
    {
        var json = JsonSerializer.Serialize(data);
        var edifact = $"UNB+UNOA:2+SENDER+RECEIVER+{DateTime.UtcNow:yyMMdd:HHmm}+1'\r\nUNH+1+{documentType}:D:96A:UN'\r\n{json}\r\nUNT+2+1'\r\nUNZ+1+1'";
        return Task.FromResult(edifact);
    }

    public Task<string> ConvertToX12Async(object data, string documentType)
    {
        var json = JsonSerializer.Serialize(data);
        var x12 = $"ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *{DateTime.UtcNow:yyMMdd}*{DateTime.UtcNow:HHmm}*U*00401*000000001*0*P*>\r\nGS*{documentType}*SENDER*RECEIVER*{DateTime.UtcNow:yyyyMMdd}*{DateTime.UtcNow:HHmm}*1*X*004010\r\n{json}\r\nGE*1*1\r\nIEA*1*000000001";
        return Task.FromResult(x12);
    }

    public Task<object> ParseEdifactAsync(string ediContent)
    {
        var lines = ediContent.Split('\'');
        var jsonLine = lines.FirstOrDefault(l => l.StartsWith("{") || l.StartsWith("["));
        if (!string.IsNullOrEmpty(jsonLine))
        {
            var obj = JsonSerializer.Deserialize<object>(jsonLine);
            return Task.FromResult(obj!)!;
        }
        return Task.FromResult<object>(new { raw = ediContent });
    }

    public Task<object> ParseX12Async(string ediContent)
    {
        var lines = ediContent.Split('\n');
        var jsonLine = lines.FirstOrDefault(l => l.StartsWith("{") || l.StartsWith("["));
        if (!string.IsNullOrEmpty(jsonLine))
        {
            var obj = JsonSerializer.Deserialize<object>(jsonLine);
            return Task.FromResult(obj!)!;
        }
        return Task.FromResult<object>(new { raw = ediContent });
    }
}
