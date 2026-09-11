using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class ProductCostingService : IProductCostingService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<ProductCostingService> _logger;

    public ProductCostingService(YuktiraDbContext db, ILogger<ProductCostingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ProductCostingResult> CalculateProductCostAsync(ProductCostingRequest request)
    {
        try
        {
            var bom = await _db.BillOfMaterials.FirstOrDefaultAsync(b => b.MaterialCode == request.MaterialCode);
            var routing = await _db.ProductionRoutings.FirstOrDefaultAsync(r => r.ProductName == request.MaterialCode);

            decimal materialCost = 0;
            decimal laborCost = 0;
            decimal overheadCost = 0;
            var components = new List<CostComponentDto>();

            if (bom != null)
            {
                materialCost = bom.Quantity * (bom.ComponentScrap > 0 ? 1 + bom.ComponentScrap / 100m : 1);
                components.Add(new CostComponentDto
                {
                    Name = bom.ComponentName,
                    Category = CostComponentCategory.MaterialCost,
                    Amount = materialCost,
                    CalculationBase = $"BOM: {bom.Quantity} {bom.UOM} @ base price"
                });
            }

            if (routing != null)
            {
                laborCost = routing.LaborTimeHrs * 50m;
                overheadCost = routing.MachineTimeHrs * 30m;

                components.Add(new CostComponentDto
                {
                    Name = "Direct Labor",
                    Category = CostComponentCategory.LaborCost,
                    Amount = laborCost,
                    CalculationBase = $"Routing: {routing.LaborTimeHrs}h @ $50/h"
                });

                components.Add(new CostComponentDto
                {
                    Name = "Machine Overhead",
                    Category = CostComponentCategory.OverheadCost,
                    Amount = overheadCost,
                    CalculationBase = $"Routing: {routing.MachineTimeHrs}h @ $30/h"
                });
            }

            var totalCost = materialCost + laborCost + overheadCost;
            var costPerUnit = request.Quantity > 0 ? totalCost / request.Quantity : 0;

            components.ForEach(c => c.Percentage = totalCost > 0 ? Math.Round(c.Amount / totalCost * 100, 1) : 0);

            _logger.LogInformation("Product cost calculated: Material={Material}, Total={Total}, PerUnit={PerUnit}",
                request.MaterialCode, totalCost, costPerUnit);

            return new ProductCostingResult
            {
                Success = true,
                MaterialCode = request.MaterialCode,
                MaterialCost = materialCost,
                LaborCost = laborCost,
                OverheadCost = overheadCost,
                TotalCost = totalCost,
                CostPerUnit = costPerUnit,
                Components = components
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product costing failed for {Material}", request.MaterialCode);
            return new ProductCostingResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<List<ProductCostingResult>> BulkCostEstimateAsync(IEnumerable<ProductCostingRequest> requests, Guid tenantId)
    {
        var results = new List<ProductCostingResult>();
        foreach (var request in requests)
        {
            results.Add(await CalculateProductCostAsync(request));
        }
        return results;
    }
}

public class ProfitabilityAnalysisService : IProfitabilityAnalysisService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<ProfitabilityAnalysisService> _logger;

    public ProfitabilityAnalysisService(YuktiraDbContext db, ILogger<ProfitabilityAnalysisService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ProfitabilityAnalysisResult> AnalyzeProfitabilityAsync(ProfitabilityAnalysisRequest request)
    {
        try
        {
            var billingDocs = await _db.BillingDocuments
                .Where(b => b.Date >= request.FromDate && b.Date <= request.ToDate)
                .ToListAsync();

            var totalRevenue = billingDocs.Sum(b => b.Amount);
            var cogs = totalRevenue * 0.65m;
            var grossMargin = totalRevenue - cogs;
            var opex = totalRevenue * 0.20m;
            var operatingProfit = grossMargin - opex;
            var netProfit = operatingProfit;

            var segments = billingDocs
                .GroupBy(b => b.CustomerName)
                .Select(g => new PaSegmentDto
                {
                    SegmentName = "Customer",
                    SegmentValue = g.Key,
                    Revenue = g.Sum(b => b.Amount),
                    Costs = g.Sum(b => b.Amount) * 0.65m,
                    Margin = g.Sum(b => b.Amount) * 0.35m,
                    MarginPercent = 35m
                })
                .ToList();

            return new ProfitabilityAnalysisResult
            {
                Success = true,
                TotalRevenue = totalRevenue,
                CostOfGoodsSold = cogs,
                GrossMargin = grossMargin,
                OperatingExpenses = opex,
                OperatingProfit = operatingProfit,
                NetProfit = netProfit,
                GrossMarginPercent = totalRevenue > 0 ? Math.Round(grossMargin / totalRevenue * 100, 1) : 0,
                OperatingMarginPercent = totalRevenue > 0 ? Math.Round(operatingProfit / totalRevenue * 100, 1) : 0,
                Segments = segments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profitability analysis failed");
            return new ProfitabilityAnalysisResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<PaSegmentDto>> GetSegmentAnalysisAsync(string segment, ProfitabilityAnalysisRequest request, Guid tenantId)
    {
        return Task.FromResult(new List<PaSegmentDto>());
    }
}

public class TransferPricingService : ITransferPricingService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<TransferPricingService> _logger;

    public TransferPricingService(YuktiraDbContext db, ILogger<TransferPricingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TransferPricingResult> CalculateTransferPriceAsync(TransferPricingRequest request)
    {
        try
        {
            var costing = new ProductCostingService(_db, null as ILogger<ProductCostingService>);
            var costResult = await costing.CalculateProductCostAsync(new ProductCostingRequest
            {
                TenantId = request.TenantId,
                MaterialCode = request.MaterialCode,
                Quantity = request.Quantity,
                CostingType = CostingType.Standard,
                CostingDate = DateTime.UtcNow
            });

            if (!costResult.Success)
                return new TransferPricingResult { Success = false, Errors = costResult.Errors };

            var costBase = costResult.CostPerUnit * request.Quantity;
            var markup = request.MarkupPercent ?? 15m;
            var markupAmount = costBase * markup / 100m;
            var transferPrice = costBase + markupAmount;

            _logger.LogInformation("Transfer price calculated: Material={Material}, Cost={Cost}, Markup={Markup}, Price={Price}",
                request.MaterialCode, costBase, markupAmount, transferPrice);

            return new TransferPricingResult
            {
                Success = true,
                TransferPrice = transferPrice,
                CostBase = costBase,
                MarkupAmount = markupAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer pricing failed");
            return new TransferPricingResult { Success = false, Errors = { ex.Message } };
        }
    }
}
