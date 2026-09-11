using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.AI.NLQuery;

[Authorize]
public class IndexModel : PageModel
{
    [BindProperty]
    public string Query { get; set; } = "";

    public string TranslatedSQL { get; set; } = "";
    public long ExecutionTime { get; set; }
    public int RowCount { get; set; }
    public List<string>? ResultColumns { get; set; }
    public List<List<string?>>? ResultRows { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            ModelState.AddModelError("Query", "Please enter a query.");
            return Page();
        }

        await Task.Delay(50);

        var rnd = new Random();
        var queryLower = Query.ToLower();

        if (queryLower.Contains("defect") && queryLower.Contains("plant"))
        {
            TranslatedSQL = "SELECT il.lot_number, il.material_code, il.plant, nr.severity, nr.defect_type, nr.detected_date " +
                "FROM inspection_lots il " +
                "JOIN non_conformances nr ON il.lot_number = nr.lot_number " +
                "WHERE il.plant = '1000' AND nr.severity IN ('High', 'Critical') " +
                "ORDER BY nr.detected_date DESC;";

            ResultColumns = new List<string> { "Lot Number", "Material", "Plant", "Severity", "Defect", "Detected Date" };
            ResultRows = new List<List<string?>>
            {
                new() { "IL-20260001", "MAT-001", "1000", "Critical", "Surface Scratch", "10 Sep 2026" },
                new() { "IL-20260002", "MAT-003", "1000", "High", "Dent", "09 Sep 2026" },
                new() { "IL-20260005", "MAT-001", "1000", "Critical", "Crack", "08 Sep 2026" },
                new() { "IL-20260007", "MAT-004", "1000", "High", "Discoloration", "07 Sep 2026" }
            };
        }
        else if (queryLower.Contains("stock"))
        {
            TranslatedSQL = "SELECT material_code, material_name, plant, storage_location, quantity, base_uom, batch_number " +
                "FROM stock_overview " +
                "WHERE quantity > 0 " +
                "ORDER BY plant, material_code;";

            ResultColumns = new List<string> { "Material", "Name", "Plant", "SLOC", "Quantity", "UoM", "Batch" };
            ResultRows = new List<List<string?>>
            {
                new() { "MAT-001", "Raw Material A", "1000", "0001", "15,200", "KG", "B-20260901" },
                new() { "MAT-002", "Raw Material B", "1000", "0002", "8,450", "KG", "B-20260903" },
                new() { "MAT-003", "Finished Good X", "2000", "0001", "3,120", "EA", "B-20260905" },
                new() { "MAT-004", "Finished Good Y", "3000", "0001", "1,870", "EA", "B-20260907" },
                new() { "MAT-005", "Packaging Material", "2000", "0003", "22,500", "EA", "B-20260908" }
            };
        }
        else if (queryLower.Contains("purchase order") || queryLower.Contains("open po"))
        {
            TranslatedSQL = "SELECT po_number, vendor_code, vendor_name, material_code, order_qty, delivery_date, status " +
                "FROM purchase_orders " +
                "WHERE status IN ('OPEN', 'PARTIALLY_RECEIVED') " +
                "ORDER BY delivery_date ASC;";

            ResultColumns = new List<string> { "PO #", "Vendor", "Vendor Name", "Material", "Qty", "Delivery Date", "Status" };
            ResultRows = new List<List<string?>>
            {
                new() { "PO-20260101", "V-001", "Acme Supplies", "MAT-001", "5,000 KG", "15 Sep 2026", "OPEN" },
                new() { "PO-20260102", "V-002", "Global Materials", "MAT-002", "3,200 KG", "18 Sep 2026", "OPEN" },
                new() { "PO-20260103", "V-001", "Acme Supplies", "MAT-005", "10,000 EA", "20 Sep 2026", "PARTIALLY_RECEIVED" }
            };
        }
        else if (queryLower.Contains("overdue") && queryLower.Contains("deliver"))
        {
            TranslatedSQL = "SELECT do_number, vendor_code, vendor_name, material_code, expected_qty, due_date, days_overdue " +
                "FROM delivery_overviews " +
                "WHERE due_date < CURRENT_DATE AND status != 'COMPLETED' " +
                "ORDER BY days_overdue DESC;";

            ResultColumns = new List<string> { "DO #", "Vendor", "Vendor Name", "Material", "Expected Qty", "Due Date", "Days Overdue" };
            ResultRows = new List<List<string?>>
            {
                new() { "DO-20260045", "V-003", "Quick Parts Inc.", "MAT-003", "2,000 EA", "01 Sep 2026", "10" },
                new() { "DO-20260048", "V-001", "Acme Supplies", "MAT-001", "4,500 KG", "05 Sep 2026", "6" },
                new() { "DO-20260051", "V-004", "Eastern Materials", "MAT-004", "1,800 EA", "08 Sep 2026", "3" }
            };
        }
        else
        {
            TranslatedSQL = "-- Generated SQL for: " + Query + "\nSELECT * FROM erp_data WHERE 1=1 LIMIT 50;";

            ResultColumns = new List<string> { "ID", "Description", "Value", "Status" };
            ResultRows = new List<List<string?>>
            {
                new() { "1", "Sample result for query", "Data here", "Active" }
            };
        }

        ExecutionTime = 20 + rnd.Next(180);
        RowCount = ResultRows.Count;

        return Page();
    }
}
