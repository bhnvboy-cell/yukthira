using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class MrpModel : PageModel
{
    private readonly IMrpService _mrpService;
    private readonly ICapacityPlanningService _capService;
    private readonly IPredictabilityService _predService;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public MrpModel(IMrpService mrpService, ICapacityPlanningService capService,
        IPredictabilityService predService, YuktiraDbContext db, ITenantContext tenant)
    {
        _mrpService = mrpService;
        _capService = capService;
        _predService = predService;
        _db = db;
        _tenant = tenant;
    }

    public List<MrpSuggestionDto> Suggestions { get; set; } = new();
    public List<WorkCenterLoadDto> CapacityLoads { get; set; } = new();
    public List<SafetyStockResult> StockAlerts { get; set; } = new();
    public DemandForecastDto? Forecast { get; set; }

    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "mrp";
    [BindProperty(SupportsGet = true)] public string? ProductName { get; set; }
    [BindProperty(SupportsGet = true)] public decimal Quantity { get; set; } = 100;
    [BindProperty(SupportsGet = true)] public string ForecastModel { get; set; } = "ExponentialSmoothing";

    public List<string> Products { get; set; } = new();

    public async Task OnGetAsync()
    {
        Products = await _db.Set<MaterialMasterEntity>().Select(m => m.Name).ToListAsync();

        if (Tab == "mrp" || string.IsNullOrEmpty(Tab))
        {
            Suggestions = await _mrpService.RunMrpAsync(_tenant.TenantId);
            StockAlerts = await _predService.GetStockAlertsAsync(_tenant.TenantId);
        }
        else if (Tab == "capacity")
        {
            CapacityLoads = await _capService.CalculateLoadAsync(_tenant.TenantId);
        }
        else if (Tab == "forecast")
        {
            StockAlerts = await _predService.CalculateAllSafetyStockAsync(_tenant.TenantId);
        }
    }

    public async Task<IActionResult> OnPostExplodeAsync()
    {
        Products = await _db.Set<MaterialMasterEntity>().Select(m => m.Name).ToListAsync();
        if (!string.IsNullOrEmpty(ProductName) && Quantity > 0)
        {
            var request = new MrpRunRequest
            {
                ProductName = ProductName,
                Quantity = Quantity,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(14)
            };
            var explosion = await _mrpService.ExplodeBomAsync(_tenant.TenantId, request);
            Suggestions = explosion.SelectMany(e => e.Requirements.Select(r => new MrpSuggestionDto
            {
                MaterialName = r.ComponentName,
                MaterialCode = r.ComponentCode,
                CurrentStock = r.OnHandStock,
                TotalDemand = r.GrossRequirement,
                ShortageQty = r.NetRequirement,
                SuggestedQty = r.NetRequirement,
                SuggestionType = r.OrderType,
                OpenPoQty = r.ScheduledReceipts
            })).ToList();
        }
        else
            Suggestions = await _mrpService.RunMrpAsync(_tenant.TenantId);

        StockAlerts = await _predService.GetStockAlertsAsync(_tenant.TenantId);
        Tab = "mrp";
        return Page();
    }

    public async Task<IActionResult> OnPostForecastAsync(Guid materialId)
    {
        Products = await _db.Set<MaterialMasterEntity>().Select(m => m.Name).ToListAsync();
        var model = ForecastModel switch
        {
            "MovingAverage" => Core.Interfaces.ForecastModel.MovingAverage,
            "WeightedMovingAverage" => Core.Interfaces.ForecastModel.WeightedMovingAverage,
            "ExponentialSmoothing" => Core.Interfaces.ForecastModel.ExponentialSmoothing,
            "LinearRegression" => Core.Interfaces.ForecastModel.LinearRegression,
            "SeasonalDecomposition" => Core.Interfaces.ForecastModel.SeasonalDecomposition,
            _ => Core.Interfaces.ForecastModel.ExponentialSmoothing
        };
        if (materialId != Guid.Empty)
            Forecast = await _predService.ForecastDemandAsync(_tenant.TenantId, materialId, 6, model);
        StockAlerts = await _predService.CalculateAllSafetyStockAsync(_tenant.TenantId);
        Tab = "forecast";
        return Page();
    }
}
