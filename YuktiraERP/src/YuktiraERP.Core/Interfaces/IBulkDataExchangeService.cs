using System;
using System.Collections.Generic;

namespace YuktiraERP.Core.Interfaces;

public class ImportRowError
{
    public int RowIndex { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ImportPreviewResult<T>
{
    public int TotalRows { get; set; }
    public int ValidRowsCount => ValidRecords.Count;
    public int ErrorRowsCount => Errors.Count;
    public bool HasErrors => Errors.Count > 0;
    public List<T> ValidRecords { get; set; } = new List<T>();
    public List<ImportRowError> Errors { get; set; } = new List<ImportRowError>();
}

public interface IBulkDataExchangeService
{
    /// <summary>
    /// Parses delimited data (CSV/TSV) for Master Items with preview and validation.
    /// Does not commit to database if preview fails or contains validation errors.
    /// </summary>
    ImportPreviewResult<MasterItemImportRow> PreviewAndValidateItems(string csvContent);

    /// <summary>
    /// Exports arbitrary records into standard RFC 4180 CSV format.
    /// </summary>
    string ExportToCsv<T>(IEnumerable<T> records, IEnumerable<string>? columnsToInclude = null);
}

public class MasterItemImportRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string ValuationMethod { get; set; } = string.Empty;
    public string BaseUomCode { get; set; } = string.Empty;
    public decimal StandardCost { get; set; }
    public bool IsBatchTracked { get; set; }
}
