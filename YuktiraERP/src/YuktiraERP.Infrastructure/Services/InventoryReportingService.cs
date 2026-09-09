using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Core.Enums;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class InventoryReportingService : IInventoryReportingService
{
    private readonly YuktiraDbContext _db;

    public InventoryReportingService(YuktiraDbContext db) => _db = db;

    public async Task<List<Mb51DocumentDto>> GetMb51DocumentsAsync(Mb51FilterDto filter, Guid tenantId)
    {
        var query = _db.MaterialDocumentHeaders
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId.ToString());

        if (!string.IsNullOrWhiteSpace(filter.DocumentNumber))
            query = query.Where(h => h.DocumentNumber.Contains(filter.DocumentNumber));

        if (filter.MovementType.HasValue)
            query = query.Where(h => h.MovementType == filter.MovementType.Value);

        if (filter.PostingDateFrom.HasValue)
            query = query.Where(h => h.PostingDate >= filter.PostingDateFrom.Value);

        if (filter.PostingDateTo.HasValue)
            query = query.Where(h => h.PostingDate <= filter.PostingDateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.PostedBy))
            query = query.Where(h => h.PostedBy == filter.PostedBy);

        var headers = await query
            .OrderByDescending(h => h.PostingDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var result = new List<Mb51DocumentDto>();
        foreach (var header in headers)
        {
            var itemQuery = _db.MaterialDocumentItems
                .AsNoTracking()
                .Where(i => i.MaterialDocumentHeaderId == header.Id.ToString());

            if (!string.IsNullOrWhiteSpace(filter.MaterialCode))
                itemQuery = itemQuery.Where(i => i.MaterialCode.Contains(filter.MaterialCode));

            if (!string.IsNullOrWhiteSpace(filter.Plant))
                itemQuery = itemQuery.Where(i => i.Plant == filter.Plant);

            if (!string.IsNullOrWhiteSpace(filter.StorageLocation))
                itemQuery = itemQuery.Where(i => i.StorageLocation == filter.StorageLocation);

            if (!string.IsNullOrWhiteSpace(filter.BatchNumber))
                itemQuery = itemQuery.Where(i => i.BatchNumber == filter.BatchNumber);

            var items = await itemQuery.ToListAsync();

            foreach (var item in items)
            {
                result.Add(new Mb51DocumentDto
                {
                    DocumentNumber = header.DocumentNumber,
                    PostingDate = header.PostingDate,
                    DocumentDate = header.DocumentDate,
                    MovementType = header.MovementType,
                    MovementTypeDescription = header.MovementTypeDescription,
                    MaterialCode = item.MaterialCode,
                    MaterialName = item.MaterialName,
                    Plant = item.Plant,
                    StorageLocation = item.StorageLocation,
                    BatchNumber = item.BatchNumber,
                    Quantity = item.Quantity,
                    UOM = item.UnitOfMeasure,
                    UnitPrice = item.UnitPrice,
                    TotalValue = item.ValuationAmount,
                    VendorCode = item.VendorCode,
                    VendorName = item.VendorName,
                    CustomerCode = item.CustomerCode,
                    CustomerName = item.CustomerName,
                    Reference = header.RefDocument,
                    HeaderText = header.HeaderText,
                    PostedBy = header.PostedBy,
                    StockBucket = item.StockBucket
                });
            }
        }

        return result;
    }

    public async Task<List<Mb52StockOverviewDto>> GetMb52StockOverviewAsync(Mb52StockOverviewFilterDto filter, Guid tenantId)
    {
        var query = _db.StockBalances
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.MaterialCode))
            query = query.Where(s => s.MaterialCode.Contains(filter.MaterialCode));

        if (!string.IsNullOrWhiteSpace(filter.Plant))
            query = query.Where(s => s.Plant == filter.Plant);

        if (!string.IsNullOrWhiteSpace(filter.StorageLocation))
            query = query.Where(s => s.StorageLocation == filter.StorageLocation);

        if (!string.IsNullOrWhiteSpace(filter.BatchNumber))
            query = query.Where(s => s.BatchNumber == filter.BatchNumber);

        if (!filter.IncludeZeroStock)
            query = query.Where(s => s.Quantity > 0);

        var balances = await query.OrderBy(s => s.MaterialCode).ThenBy(s => s.Plant).ToListAsync();

        IEnumerable<IGrouping<string, StockBalanceEntity>> stringKeyed;
        Func<StockBalanceEntity, string> keySelector = filter.GroupBy switch
        {
            MbStockOverviewLevel.Plant => s => $"{s.MaterialCode}|{s.MaterialName}|{s.Plant}",
            MbStockOverviewLevel.StorageLocation => s => $"{s.MaterialCode}|{s.MaterialName}|{s.Plant}|{s.StorageLocation}",
            _ => s => $"{s.MaterialCode}|{s.MaterialName}|{s.Plant}|{s.StorageLocation}|{s.BatchNumber}"
        };

        var grouped = balances.GroupBy(keySelector);

        return grouped.Select(g => new Mb52StockOverviewDto
        {
            MaterialCode = g.First().MaterialCode,
            MaterialName = g.First().MaterialName,
            Plant = g.First().Plant,
            StorageLocation = g.First().StorageLocation,
            BatchNumber = g.First().BatchNumber,
            UnrestrictedQty = g.Where(s => s.StockType == "Unrestricted").Sum(s => s.Quantity),
            QualityInspectionQty = g.Where(s => s.StockType == "QualityInspection").Sum(s => s.Quantity),
            BlockedQty = g.Where(s => s.StockType == "Blocked").Sum(s => s.Quantity),
            GRBlockedQty = g.Where(s => s.StockType == "GRBlocked").Sum(s => s.Quantity),
            ReservedQty = g.Where(s => s.StockType == "Reserved").Sum(s => s.Quantity),
            TotalQty = g.Sum(s => s.Quantity),
            UnitPrice = g.Average(s => s.UnitPrice),
            TotalValue = g.Sum(s => s.TotalValue),
            UOM = g.First().UOM,
            MinStock = g.First().MinStock,
            MaxStock = g.First().MaxStock,
            IsBelowMinStock = g.Sum(s => s.Quantity) < g.First().MinStock && g.First().MinStock > 0,
            IsAboveMaxStock = g.Sum(s => s.Quantity) > g.First().MaxStock && g.First().MaxStock > 0
        }).ToList();
    }

    public async Task<List<Mb5BHistoricalStockDto>> GetMb5BHistoricalStockAsync(string materialCode, string plant, DateTime fromDate, DateTime toDate, Guid tenantId)
    {
        var movements = await _db.StockMovementHistory
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId.ToString() &&
                m.MaterialCode == materialCode &&
                m.Plant == plant &&
                m.MovementDate >= fromDate &&
                m.MovementDate <= toDate)
            .OrderBy(m => m.MovementDate)
            .ToListAsync();

        var balances = await _db.StockBalances
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId &&
                s.MaterialCode == materialCode &&
                s.Plant == plant)
            .ToListAsync();

        var currentQty = balances.Sum(s => s.Quantity);
        var currentValue = balances.Sum(s => s.TotalValue);

        var receipts = movements.Where(m => m.MovementType is 101 or 103 or 561 or 601).ToList();
        var issues = movements.Where(m => m.MovementType is 201 or 261 or 281 or 601).ToList();

        return new List<Mb5BHistoricalStockDto>
        {
            new Mb5BHistoricalStockDto
            {
                MaterialCode = materialCode,
                Plant = plant,
                ValuationDate = toDate,
                OpeningQuantity = currentQty - receipts.Sum(r => r.Quantity) + issues.Sum(i => i.Quantity),
                OpeningValue = currentValue - receipts.Sum(r => r.TotalValue) + issues.Sum(i => i.TotalValue),
                ReceiptQuantity = receipts.Sum(r => r.Quantity),
                ReceiptValue = receipts.Sum(r => r.TotalValue),
                IssueQuantity = issues.Sum(i => i.Quantity),
                IssueValue = issues.Sum(i => i.TotalValue),
                ClosingQuantity = currentQty,
                ClosingValue = currentValue,
                MovingAveragePrice = currentQty > 0 ? currentValue / currentQty : 0,
                DailyMovements = movements.GroupBy(m => m.MovementDate.Date).Select(g => new Mb5BDailyMovementDto
                {
                    Date = g.Key,
                    MovementType = g.First().MovementType,
                    Description = g.First().MovementTypeDescription,
                    Quantity = g.Sum(m => m.Quantity),
                    Value = g.Sum(m => m.TotalValue),
                    RunningBalance = g.Sum(m => m.Quantity),
                    RunningValue = g.Sum(m => m.TotalValue)
                }).ToList()
            }
        };
    }

    public async Task<List<Mb5LReconciliationDto>> GetMb5LReconciliationAsync(string? materialCode, string? plant, Guid tenantId)
    {
        var query = _db.StockBalances
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(materialCode))
            query = query.Where(s => s.MaterialCode.Contains(materialCode));
        if (!string.IsNullOrWhiteSpace(plant))
            query = query.Where(s => s.Plant == plant);

        var balances = await query.ToListAsync();

        var grouped = balances.GroupBy(s => new { s.MaterialCode, s.MaterialName, s.Plant, s.StorageLocation, s.BatchNumber });

        var result = new List<Mb5LReconciliationDto>();
        foreach (var g in grouped)
        {
            var mmQty = g.Sum(s => s.Quantity);
            var mmValue = g.Sum(s => s.TotalValue);

            decimal fiDebit = 0;
            decimal fiCredit = 0;
            try
            {
                var fiEntries = await _db.UniversalJournals
                    .AsNoTracking()
                    .Where(j =>
                        j.TenantId == tenantId &&
                        j.MaterialCode == g.Key.MaterialCode &&
                        j.Plant == g.Key.Plant)
                    .ToListAsync();

                fiDebit = fiEntries.Sum(j => j.DebitAmount);
                fiCredit = fiEntries.Sum(j => j.CreditAmount);
            }
            catch
            {
                // UniversalJournals table may not exist yet
            }

            var fiBalance = fiDebit - fiCredit;
            var difference = mmValue - fiBalance;

            result.Add(new Mb5LReconciliationDto
            {
                MaterialCode = g.Key.MaterialCode,
                MaterialName = g.Key.MaterialName,
                Plant = g.Key.Plant,
                StorageLocation = g.Key.StorageLocation,
                BatchNumber = g.Key.BatchNumber,
                MmStockQuantity = mmQty,
                MmStockValue = mmValue,
                FiGlDebit = fiDebit,
                FiGlCredit = fiCredit,
                FiGlBalance = fiBalance,
                Difference = difference,
                IsReconciled = Math.Abs(difference) < 0.01m,
                DiscrepancyReason = Math.Abs(difference) >= 0.01m ? "MM-FI value mismatch" : null
            });
        }

        return result;
    }

    public async Task<byte[]> ExportToExcelAsync(MbExcelExportRequestDto request, Guid tenantId)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add(request.ReportType);

        var headers = request.ReportType switch
        {
            "MB51" => new[] { "Document #", "Posting Date", "Movement Type", "Description", "Material", "Plant", "SLoc", "Batch", "Qty", "UOM", "Value", "Reference", "Posted By" },
            "MB52" => new[] { "Material", "Name", "Plant", "SLoc", "Batch", "Unrestricted", "QI", "Blocked", "GR Blocked", "Reserved", "Total", "Unit Price", "Total Value" },
            "MB5B" => new[] { "Material", "Plant", "Valuation Date", "Opening Qty", "Opening Value", "Receipts Qty", "Receipts Value", "Issues Qty", "Issues Value", "Closing Qty", "Closing Value", "MAP" },
            "MB5L" => new[] { "Material", "Name", "Plant", "SLoc", "Batch", "MM Qty", "MM Value", "FI Debit", "FI Credit", "FI Balance", "Difference", "Reconciled" },
            _ => new[] { "Column 1", "Column 2" }
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        }

        if (request.FreezePanes)
            worksheet.SheetView.FreezeRows(1);

        int row = 2;
        if (request.ReportType == "MB51" && request.Mb51Filter != null)
        {
            var docs = await GetMb51DocumentsAsync(request.Mb51Filter, tenantId);
            foreach (var doc in docs)
            {
                worksheet.Cell(row, 1).Value = doc.DocumentNumber;
                worksheet.Cell(row, 2).Value = doc.PostingDate;
                worksheet.Cell(row, 3).Value = doc.MovementType;
                worksheet.Cell(row, 4).Value = doc.MovementTypeDescription;
                worksheet.Cell(row, 5).Value = doc.MaterialCode;
                worksheet.Cell(row, 6).Value = doc.Plant;
                worksheet.Cell(row, 7).Value = doc.StorageLocation;
                worksheet.Cell(row, 8).Value = doc.BatchNumber ?? "";
                worksheet.Cell(row, 9).Value = (double)doc.Quantity;
                worksheet.Cell(row, 10).Value = doc.UOM;
                worksheet.Cell(row, 11).Value = (double)doc.TotalValue;
                worksheet.Cell(row, 12).Value = doc.Reference ?? "";
                worksheet.Cell(row, 13).Value = doc.PostedBy ?? "";
                row++;
            }
        }
        else if (request.ReportType == "MB52" && request.Mb52Filter != null)
        {
            var stocks = await GetMb52StockOverviewAsync(request.Mb52Filter, tenantId);
            foreach (var stock in stocks)
            {
                worksheet.Cell(row, 1).Value = stock.MaterialCode;
                worksheet.Cell(row, 2).Value = stock.MaterialName;
                worksheet.Cell(row, 3).Value = stock.Plant;
                worksheet.Cell(row, 4).Value = stock.StorageLocation;
                worksheet.Cell(row, 5).Value = stock.BatchNumber ?? "";
                worksheet.Cell(row, 6).Value = (double)stock.UnrestrictedQty;
                worksheet.Cell(row, 7).Value = (double)stock.QualityInspectionQty;
                worksheet.Cell(row, 8).Value = (double)stock.BlockedQty;
                worksheet.Cell(row, 9).Value = (double)stock.GRBlockedQty;
                worksheet.Cell(row, 10).Value = (double)stock.ReservedQty;
                worksheet.Cell(row, 11).Value = (double)stock.TotalQty;
                worksheet.Cell(row, 12).Value = (double)stock.UnitPrice;
                worksheet.Cell(row, 13).Value = (double)stock.TotalValue;
                row++;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
