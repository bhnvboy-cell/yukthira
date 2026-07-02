namespace YuktiraERP.Core.Interfaces;

public enum ExportFormat { Xlsx, Csv, Txt, Pdf, Html }

public interface IExportService
{
    Task<byte[]> ExportGridAsync<T>(List<T> data, ExportFormat format, string? fileName = null);
    Task<byte[]> ExportSelectedAsync<T>(List<T> data, List<string> columns, ExportFormat format);
    Task<byte[]> ExportFilteredAsync<T>(List<T> data, string filterExpression, ExportFormat format);
    Task<byte[]> GenerateDocumentAsync(string templateCode, Dictionary<string, object> data, string format = "PDF");
}
