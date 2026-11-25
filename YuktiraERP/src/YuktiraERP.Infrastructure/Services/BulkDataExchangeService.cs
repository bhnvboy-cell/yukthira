using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using YuktiraERP.Core.Domain.MasterData;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class BulkDataExchangeService : IBulkDataExchangeService
{
    public ImportPreviewResult<MasterItemImportRow> PreviewAndValidateItems(string csvContent)
    {
        var result = new ImportPreviewResult<MasterItemImportRow>();

        if (string.IsNullOrWhiteSpace(csvContent))
            return result;

        using var reader = new StringReader(csvContent);
        string? headerLine = reader.ReadLine();
        if (headerLine == null)
            return result;

        var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        int codeIdx = Array.IndexOf(headers, "code");
        int nameIdx = Array.IndexOf(headers, "name");
        int classIdx = Array.IndexOf(headers, "classification");
        int valIdx = Array.IndexOf(headers, "valuationmethod");
        int uomIdx = Array.IndexOf(headers, "baseuomcode");
        int costIdx = Array.IndexOf(headers, "standardcost");
        int batchIdx = Array.IndexOf(headers, "isbatchtracked");

        if (codeIdx < 0 || nameIdx < 0 || uomIdx < 0)
        {
            result.Errors.Add(new ImportRowError
            {
                RowIndex = 0,
                FieldName = "Header",
                ErrorMessage = "CSV missing mandatory columns: Code, Name, BaseUomCode."
            });
            return result;
        }

        int rowNum = 1;
        string? line;
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while ((line = reader.ReadLine()) != null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRows++;
            var cols = line.Split(',').Select(c => c.Trim()).ToArray();
            bool hasRowError = false;

            string code = cols.Length > codeIdx ? cols[codeIdx] : string.Empty;
            string name = cols.Length > nameIdx ? cols[nameIdx] : string.Empty;
            string uom = cols.Length > uomIdx ? cols[uomIdx] : string.Empty;

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Errors.Add(new ImportRowError { RowIndex = rowNum, FieldName = "Code", ErrorMessage = "Item code cannot be blank." });
                hasRowError = true;
            }
            else if (!seenCodes.Add(code))
            {
                result.Errors.Add(new ImportRowError { RowIndex = rowNum, FieldName = "Code", RawValue = code, ErrorMessage = $"Duplicate Item code '{code}' in upload." });
                hasRowError = true;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add(new ImportRowError { RowIndex = rowNum, FieldName = "Name", ErrorMessage = "Item name cannot be blank." });
                hasRowError = true;
            }

            if (string.IsNullOrWhiteSpace(uom))
            {
                result.Errors.Add(new ImportRowError { RowIndex = rowNum, FieldName = "BaseUomCode", ErrorMessage = "Base UOM code cannot be blank." });
                hasRowError = true;
            }

            decimal standardCost = 0m;
            if (costIdx >= 0 && cols.Length > costIdx && !string.IsNullOrWhiteSpace(cols[costIdx]))
            {
                if (!decimal.TryParse(cols[costIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out standardCost) || standardCost < 0)
                {
                    result.Errors.Add(new ImportRowError { RowIndex = rowNum, FieldName = "StandardCost", RawValue = cols[costIdx], ErrorMessage = "Invalid Standard Cost value." });
                    hasRowError = true;
                }
            }

            bool isBatch = false;
            if (batchIdx >= 0 && cols.Length > batchIdx && !string.IsNullOrWhiteSpace(cols[batchIdx]))
            {
                bool.TryParse(cols[batchIdx], out isBatch);
            }

            string classification = (classIdx >= 0 && cols.Length > classIdx) ? cols[classIdx] : "TradingGood";
            string valuationMethod = (valIdx >= 0 && cols.Length > valIdx) ? cols[valIdx] : "MovingAverage";

            if (!hasRowError)
            {
                result.ValidRecords.Add(new MasterItemImportRow
                {
                    Code = code,
                    Name = name,
                    BaseUomCode = uom,
                    StandardCost = standardCost,
                    IsBatchTracked = isBatch,
                    Classification = classification,
                    ValuationMethod = valuationMethod
                });
            }
        }

        return result;
    }

    public string ExportToCsv<T>(IEnumerable<T> records, IEnumerable<string>? columnsToInclude = null)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        if (columnsToInclude != null)
        {
            var colSet = new HashSet<string>(columnsToInclude, StringComparer.OrdinalIgnoreCase);
            properties = properties.Where(p => colSet.Contains(p.Name)).ToList();
        }

        // Header
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p.Name))));

        // Rows
        foreach (var item in records)
        {
            var values = properties.Select(p =>
            {
                var val = p.GetValue(item);
                return EscapeCsvValue(val?.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
