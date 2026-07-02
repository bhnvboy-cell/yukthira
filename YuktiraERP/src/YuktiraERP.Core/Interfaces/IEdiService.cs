namespace YuktiraERP.Core.Interfaces;

public interface IEdiService
{
    Task<string> ConvertToEdifactAsync(object data, string documentType);
    Task<string> ConvertToX12Async(object data, string documentType);
    Task<object> ParseEdifactAsync(string ediContent);
    Task<object> ParseX12Async(string ediContent);
}
