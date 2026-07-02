using System.Globalization;
using System.Text;
using System.Text.Json;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.ExportEngine;

public class ExportService : IExportService
{
    public Task<byte[]> ExportGridAsync<T>(List<T> data, ExportFormat format, string? fileName = null)
    {
        return format switch
        {
            ExportFormat.Xlsx => ExportToXlsx(data),
            ExportFormat.Csv => ExportToCsv(data),
            ExportFormat.Txt => ExportToTxt(data),
            ExportFormat.Pdf => ExportToPdf(data),
            _ => ExportToCsv(data)
        };
    }

    public Task<byte[]> ExportSelectedAsync<T>(List<T> data, List<string> columns, ExportFormat format)
    {
        return ExportGridAsync(data, format);
    }

    public Task<byte[]> ExportFilteredAsync<T>(List<T> data, string filterExpression, ExportFormat format)
    {
        return ExportGridAsync(data, format);
    }

    public async Task<byte[]> GenerateDocumentAsync(string templateCode, Dictionary<string, object> data, string format = "PDF")
    {
        var html = GenerateHtmlTemplate(templateCode, data);
        var pdfBytes = ConvertHtmlToPdf(html);
        return await Task.FromResult(pdfBytes);
    }

    private async Task<byte[]> ExportToXlsx<T>(List<T> data)
    {
        var items = ConvertToDictionaryList(data);
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Export");
        if (items.Count > 0)
        {
            var headers = items[0].Keys.ToList();
            for (int c = 0; c < headers.Count; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }
            for (int r = 0; r < items.Count; r++)
            {
                for (int c = 0; c < headers.Count; c++)
                {
                    var val = items[r][headers[c]]?.ToString() ?? "";
                    ws.Cell(r + 2, c + 1).Value = val;
                }
            }
            ws.Columns().AdjustToContents();
        }
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static Task<byte[]> ExportToCsv<T>(List<T> data)
    {
        var items = ConvertToDictionaryList(data);
        var sb = new StringBuilder();
        if (items.Count > 0)
        {
            var headers = items[0].Keys.ToList();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var item in items)
                sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(item[h]?.ToString() ?? ""))));
        }
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static Task<byte[]> ExportToTxt<T>(List<T> data)
    {
        var items = ConvertToDictionaryList(data);
        var sb = new StringBuilder();
        foreach (var item in items)
            sb.AppendLine(JsonSerializer.Serialize(item));
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static Task<byte[]> ExportToPdf<T>(List<T> data)
    {
        var items = ConvertToDictionaryList(data);
        var html = new StringBuilder();
        html.AppendLine("<html><head><style>table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background-color:#1a237e;color:white}</style></head><body>");
        html.AppendLine("<h2>Yuktira ERP Export</h2><table>");
        if (items.Count > 0)
        {
            html.AppendLine("<tr>" + string.Join("", items[0].Keys.Select(k => $"<th>{k}</th>")) + "</tr>");
            foreach (var item in items)
                html.AppendLine("<tr>" + string.Join("", item.Values.Select(v => $"<td>{v}</td>")) + "</tr>");
        }
        html.AppendLine("</table></body></html>");
        var bytes = ConvertHtmlToPdf(html.ToString());
        return Task.FromResult(bytes);
    }

    private static string GenerateHtmlTemplate(string templateCode, Dictionary<string, object> data)
    {
        var html = templateCode switch
        {
            "PO" => GeneratePoHtml(data),
            "SO" => GenerateSoHtml(data),
            "INVOICE" => GenerateInvoiceHtml(data),
            "COA" => GenerateCoaHtml(data),
            "GRN" => GenerateGrnHtml(data),
            "PROD_ORDER" => GenerateProdOrderHtml(data),
            "QC_REPORT" => GenerateQcReportHtml(data),
            "PAYSLIP" => GeneratePayslipHtml(data),
            "FIN_STMT" => GenerateFinStmtHtml(data),
            _ => GenerateDefaultHtml(data)
        };
        return html;
    }

    private static string GeneratePoHtml(Dictionary<string, object> data) => $@"
<html><body style='font-family:Arial;padding:40px'>
<h1 style='color:#1a237e;border-bottom:3px solid #1a237e'>PURCHASE ORDER</h1>
<div style='display:flex;justify-content:space-between;margin:20px 0'>
<div><strong>PO Number:</strong> {GetValue(data, "po_number")}<br/>
<strong>Date:</strong> {GetValue(data, "po_date")}</div>
<div><strong>Vendor:</strong> {GetValue(data, "vendor_name")}<br/>
<strong>Payment Terms:</strong> {GetValue(data, "payment_terms")}</div>
</div>
<table style='width:100%;border-collapse:collapse;margin:20px 0'>
<thead><tr style='background:#1a237e;color:white'><th>Item</th><th>Material</th><th>Qty</th><th>UOM</th><th>Price</th><th>Total</th></tr></thead>
<tbody><tr><td colspan='5' style='text-align:right'><strong>Total:</strong></td><td>{GetValue(data, "grand_total")}</td></tr></tbody>
</table>
<p style='margin-top:40px'><strong>Terms & Conditions:</strong> Standard purchase terms apply.</p>
</body></html>";

    private static string GenerateSoHtml(Dictionary<string, object> data) => $@"
<html><body style='font-family:Arial;padding:40px'>
<h1 style='color:#1a237e;border-bottom:3px solid #1a237e'>SALES ORDER</h1>
<div style='display:flex;justify-content:space-between;margin:20px 0'>
<div><strong>SO Number:</strong> {GetValue(data, "so_number")}<br/>
<strong>Date:</strong> {GetValue(data, "so_date")}</div>
<div><strong>Customer:</strong> {GetValue(data, "customer_name")}<br/>
<strong>Delivery Date:</strong> {GetValue(data, "delivery_date")}</div>
</div>
<table style='width:100%;border-collapse:collapse;margin:20px 0'>
<thead><tr style='background:#1a237e;color:white'><th>#</th><th>Material</th><th>Qty</th><th>Price</th><th>Total</th></tr></thead>
<tbody><tr><td colspan='4' style='text-align:right'><strong>Grand Total:</strong></td><td>{GetValue(data, "grand_total")}</td></tr></tbody>
</table>
</body></html>";

    private static string GenerateInvoiceHtml(Dictionary<string, object> data) => $@"
<html><body style='font-family:Arial;padding:40px'>
<h1 style='color:#1a237e;border-bottom:3px solid #1a237e'>INVOICE</h1>
<div style='display:flex;justify-content:space-between;margin:20px 0'>
<div><strong>Invoice No:</strong> {GetValue(data, "billing_number")}<br/>
<strong>Date:</strong> {GetValue(data, "billing_date")}</div>
<div><strong>Customer:</strong> {GetValue(data, "customer_name")}</div>
</div>
<table style='width:100%;border-collapse:collapse;margin:20px 0'>
<thead><tr style='background:#1a237e;color:white'><th>Description</th><th>Amount</th></tr></thead>
<tbody><tr><td><strong>Total</strong></td><td>{GetValue(data, "grand_total")}</td></tr></tbody>
</table>
</body></html>";

    private static string GenerateCoaHtml(Dictionary<string, object> data) => $@"
<html><body style='font-family:Arial;padding:40px'>
<h1 style='color:#1a237e;border-bottom:3px solid #1a237e'>CERTIFICATE OF ANALYSIS</h1>
<p><strong>COA Number:</strong> {GetValue(data, "coa_number")}</p>
<p><strong>Material:</strong> {GetValue(data, "material_name")} ({GetValue(data, "material_code")})</p>
<p><strong>Batch:</strong> {GetValue(data, "batch_no")}</p>
<table style='width:100%;border-collapse:collapse;margin:20px 0'>
<thead><tr style='background:#1a237e;color:white'><th>Parameter</th><th>Specification</th><th>Result</th><th>Status</th></tr></thead>
<tbody><tr><td colspan='4'>Results conform to specification</td></tr></tbody>
</table>
</body></html>";

    private static string GenerateGrnHtml(Dictionary<string, object> data) => $@"
<html><head><style>
body{{font-family:Arial;padding:30px}}h1{{color:#1a237e;border-bottom:3px solid #1a237e;padding-bottom:10px}}
.header{{display:flex;justify-content:space-between;margin:20px 0}}
table{{width:100%;border-collapse:collapse;margin:20px 0}}
th{{background:#1a237e;color:white;padding:10px;text-align:left;font-size:13px}}
td{{padding:8px;border:1px solid #ddd;font-size:13px}}
.footer{{margin-top:40px;display:flex;justify-content:space-between}}
.signature{{text-align:center;margin-top:60px}}
</style></head><body>
<h1>GOODS RECEIPT NOTE</h1>
<div class='header'>
<div><strong>GRN Number:</strong> {GetValue(data, "grn_number")}<br/><strong>Date:</strong> {GetValue(data, "grn_date")}</div>
<div><strong>Company:</strong> {GetValue(data, "company_name")}<br/><strong>PO Reference:</strong> {GetValue(data, "po_reference")}</div>
</div>
<table><thead><tr><th>Item Code</th><th>Description</th><th>Ordered Qty</th><th>Received Qty</th><th>Accepted Qty</th><th>Rejected Qty</th><th>Remarks</th></tr></thead>
<tbody><tr><td colspan='7' style='text-align:center;color:#999'>Line items data</td></tr></tbody></table>
<div class='footer'>
<div class='signature'><strong>Received by:</strong> _________________</div>
<div class='signature'><strong>Verified by:</strong> _________________</div>
<div class='signature'><strong>Approved by:</strong> _________________</div>
</div>
</body></html>";

    private static string GenerateProdOrderHtml(Dictionary<string, object> data) => $@"
<html><head><style>
body{{font-family:Arial;padding:30px}}h1{{color:#1a237e;border-bottom:3px solid #1a237e;padding-bottom:10px}}
.header{{display:flex;justify-content:space-between;margin:20px 0}}
table{{width:100%;border-collapse:collapse;margin:20px 0}}
th{{background:#1a237e;color:white;padding:10px;text-align:left;font-size:13px}}
td{{padding:8px;border:1px solid #ddd;font-size:13px}}
h3{{color:#1a237e;margin-top:30px}}
</style></head><body>
<h1>PRODUCTION ORDER</h1>
<div class='header'>
<div><strong>Order No:</strong> {GetValue(data, "order_number")}<br/><strong>Product:</strong> {GetValue(data, "product_name")}</div>
<div><strong>Quantity:</strong> {GetValue(data, "quantity")}<br/><strong>Start Date:</strong> {GetValue(data, "start_date")}<br/><strong>End Date:</strong> {GetValue(data, "end_date")}</div>
</div>
<h3>Components</h3>
<table><thead><tr><th>Material Code</th><th>Name</th><th>Required Qty</th><th>Issued Qty</th><th>Shortage</th></tr></thead>
<tbody><tr><td colspan='5' style='text-align:center;color:#999'>Component data</td></tr></tbody></table>
<h3>Routing Operations</h3>
<table><thead><tr><th>Operation</th><th>Work Center</th><th>Setup Time (hrs)</th><th>Run Time (hrs)</th></tr></thead>
<tbody><tr><td colspan='4' style='text-align:center;color:#999'>Routing data</td></tr></tbody></table>
</body></html>";

    private static string GenerateQcReportHtml(Dictionary<string, object> data) => $@"
<html><head><style>
body{{font-family:Arial;padding:30px}}h1{{color:#1a237e;border-bottom:3px solid #1a237e;padding-bottom:10px}}
.header{{margin:20px 0}}table{{width:100%;border-collapse:collapse;margin:20px 0}}
th{{background:#1a237e;color:white;padding:10px;text-align:left;font-size:13px}}
td{{padding:8px;border:1px solid #ddd;font-size:13px}}.pass{{color:green;font-weight:bold}}.fail{{color:red;font-weight:bold}}
.footer{{margin-top:40px}}
</style></head><body>
<h1>QUALITY CONTROL REPORT</h1>
<div class='header'>
<div><strong>Report No:</strong> {GetValue(data, "report_number")}<br/><strong>Material:</strong> {GetValue(data, "material_name")}<br/><strong>Batch/Lot:</strong> {GetValue(data, "batch_lot")}<br/><strong>Date:</strong> {GetValue(data, "inspection_date")}<br/><strong>Inspector:</strong> {GetValue(data, "inspector")}</div>
</div>
<table><thead><tr><th>Parameter</th><th>Specification</th><th>Result</th><th>Status</th></tr></thead>
<tbody><tr><td colspan='4' style='text-align:center;color:#999'>Inspection parameters data</td></tr></tbody></table>
<div class='footer'>
<p><strong>Overall Verdict:</strong> {GetValue(data, "verdict")}</p>
<p><strong>Inspector Signature:</strong> _________________</p>
<p><strong>Review Date:</strong> {GetValue(data, "review_date")}</p>
</div>
</body></html>";

    private static string GeneratePayslipHtml(Dictionary<string, object> data) => $@"
<html><head><style>
body{{font-family:Arial;padding:30px}}h1{{color:#1a237e;border-bottom:3px solid #1a237e;padding-bottom:10px}}
.header{{display:flex;justify-content:space-between;margin:20px 0}}
table{{width:100%;border-collapse:collapse;margin:15px 0}}
th{{background:#1a237e;color:white;padding:8px;text-align:left;font-size:13px}}
td{{padding:6px;border:1px solid #ddd;font-size:13px}}
.net-pay{{background:#e8eaf6;font-size:18px;font-weight:bold;text-align:center;padding:15px;margin:20px 0}}
.footer{{margin-top:30px;font-size:12px;color:#666}}
</style></head><body>
<h1>PAY SLIP</h1>
<div class='header'>
<div><strong>Company:</strong> {GetValue(data, "company_name")}<br/><strong>Employee:</strong> {GetValue(data, "employee_name")}<br/><strong>Code:</strong> {GetValue(data, "employee_code")}</div>
<div><strong>Department:</strong> {GetValue(data, "department")}<br/><strong>Pay Period:</strong> {GetValue(data, "pay_period")}<br/><strong>Pay Date:</strong> {GetValue(data, "pay_date")}</div>
</div>
<h3>Earnings</h3>
<table><thead><tr><th>Component</th><th>Amount</th></tr></thead>
<tbody><tr><td>Basic</td><td>{GetValue(data, "basic")}</td></tr><tr><td>HRA</td><td>{GetValue(data, "hra")}</td></tr><tr><td>DA</td><td>{GetValue(data, "da")}</td></tr><tr><td>Conveyance</td><td>{GetValue(data, "conveyance")}</td></tr><tr><td>Medical</td><td>{GetValue(data, "medical")}</td></tr><tr><td>Special Allowance</td><td>{GetValue(data, "special_allowance")}</td></tr><tr style='font-weight:bold;background:#f5f5f5'><td>Gross Earnings</td><td>{GetValue(data, "gross_earnings")}</td></tr></tbody></table>
<h3>Deductions</h3>
<table><thead><tr><th>Component</th><th>Amount</th></tr></thead>
<tbody><tr><td>PF</td><td>{GetValue(data, "pf")}</td></tr><tr><td>ESI</td><td>{GetValue(data, "esi")}</td></tr><tr><td>Professional Tax</td><td>{GetValue(data, "professional_tax")}</td></tr><tr><td>TDS</td><td>{GetValue(data, "tds")}</td></tr><tr><td>Loan Recovery</td><td>{GetValue(data, "loan_recovery")}</td></tr><tr style='font-weight:bold;background:#f5f5f5'><td>Total Deductions</td><td>{GetValue(data, "total_deductions")}</td></tr></tbody></table>
<div class='net-pay'>Net Pay: {GetValue(data, "net_pay")}</div>
<p><strong>Net Pay (in words):</strong> {GetValue(data, "net_pay_words")}</p>
<div class='footer'>
<p><strong>Bank Details:</strong> {GetValue(data, "bank_details")}</p>
<p><strong>Generated Date:</strong> {GetValue(data, "generated_date")}</p>
<p>This is a computer-generated payslip and does not require a signature.</p>
</div>
</body></html>";

    private static string GenerateFinStmtHtml(Dictionary<string, object> data) => $@"
<html><head><style>
body{{font-family:Arial;padding:30px}}h1{{color:#1a237e;border-bottom:3px solid #1a237e;padding-bottom:10px}}
.header{{margin:20px 0}}table{{width:100%;border-collapse:collapse;margin:20px 0}}
th{{background:#1a237e;color:white;padding:10px;text-align:left;font-size:13px}}
td{{padding:8px;border:1px solid #ddd;font-size:13px}}
.total-row{{font-weight:bold;background:#e8eaf6}}
h3{{color:#1a237e;margin-top:25px}}
</style></head><body>
<h1>{(GetValue(data, "statement_type") == "PROFIT_LOSS" ? "PROFIT & LOSS STATEMENT" : "BALANCE SHEET")}</h1>
<div class='header'>
<p><strong>Company:</strong> {GetValue(data, "company_name")}<br/><strong>Period:</strong> {GetValue(data, "period")}<br/><strong>Date:</strong> {GetValue(data, "statement_date")}</p>
</div>
{(GetValue(data, "statement_type") == "PROFIT_LOSS" ? $@"
<h3>Revenue</h3>
<table><thead><tr><th>Description</th><th>Amount</th></tr></thead>
<tbody><tr><td>Revenue</td><td>{GetValue(data, "revenue")}</td></tr><tr><td>COGS</td><td>({GetValue(data, "cogs")})</td></tr><tr class='total-row'><td>Gross Profit</td><td>{GetValue(data, "gross_profit")}</td></tr></tbody></table>
<h3>Expenses</h3>
<table><thead><tr><th>Description</th><th>Amount</th></tr></thead>
<tbody><tr><td>Operating Expenses</td><td>{GetValue(data, "operating_expenses")}</td></tr><tr><td>Interest</td><td>{GetValue(data, "interest")}</td></tr><tr><td>Depreciation</td><td>{GetValue(data, "depreciation")}</td></tr><tr><td>Tax</td><td>{GetValue(data, "tax")}</td></tr><tr class='total-row'><td>Net Profit</td><td>{GetValue(data, "net_profit")}</td></tr></tbody></table>
" : $@"
<h3>Assets</h3>
<table><thead><tr><th>Description</th><th>Amount</th></tr></thead>
<tbody><tr><td>Fixed Assets</td><td>{GetValue(data, "fixed_assets")}</td></tr><tr><td>Current Assets</td><td>{GetValue(data, "current_assets")}</td></tr><tr class='total-row'><td>Total Assets</td><td>{GetValue(data, "total_assets")}</td></tr></tbody></table>
<h3>Liabilities & Equity</h3>
<table><thead><tr><th>Description</th><th>Amount</th></tr></thead>
<tbody><tr><td>Liabilities</td><td>{GetValue(data, "liabilities")}</td></tr><tr><td>Equity</td><td>{GetValue(data, "equity")}</td></tr><tr class='total-row'><td>Total Liabilities & Equity</td><td>{GetValue(data, "total_liabilities_equity")}</td></tr></tbody></table>
")}
</body></html>";

    private static string GenerateDefaultHtml(Dictionary<string, object> data) => $@"
<html><body style='font-family:Arial;padding:40px'>
<h1>Yuktira ERP Document</h1>
{string.Join("", data.Select(kv => $"<p><strong>{kv.Key}:</strong> {kv.Value}</p>"))}
</body></html>";

    private static byte[] ConvertHtmlToPdf(string html)
    {
        try
        {
            var converter = new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools());
            var doc = new DinkToPdf.HtmlToPdfDocument
            {
                GlobalSettings = { PaperSize = DinkToPdf.PaperKind.A4, Orientation = DinkToPdf.Orientation.Portrait },
                Objects = { new DinkToPdf.ObjectSettings { HtmlContent = html } }
            };
            return converter.Convert(doc);
        }
        catch
        {
            return Encoding.UTF8.GetBytes(html);
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string GetValue(Dictionary<string, object> data, string key)
        => data.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";

    private static List<Dictionary<string, object?>> ConvertToDictionaryList<T>(List<T> data)
    {
        return data.Select(item =>
        {
            if (item is Dictionary<string, object> d)
                return d.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            var dict = new Dictionary<string, object?>();
            foreach (var prop in typeof(T).GetProperties())
                dict[prop.Name] = prop.GetValue(item);
            return dict;
        }).ToList();
    }
}
