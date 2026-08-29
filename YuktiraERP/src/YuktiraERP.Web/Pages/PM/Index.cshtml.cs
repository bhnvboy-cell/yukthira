using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<EquipmentEntity, Guid> _equipRepo;
    private readonly IRepository<FunctionalLocationEntity, Guid> _locRepo;
    private readonly IRepository<MaintenanceNotificationEntity, Guid> _notifRepo;
    private readonly IRepository<MaintenanceOrderEntity, Guid> _orderRepo;
    private readonly IRepository<MaintenancePlanEntity, Guid> _planRepo;
    private readonly IRepository<SparePartEntity, Guid> _spareRepo;

    public List<EquipmentEntity> Equipments { get; set; } = new();
    public List<FunctionalLocationEntity> Locations { get; set; } = new();
    public List<MaintenanceNotificationEntity> Notifications { get; set; } = new();
    public List<MaintenanceOrderEntity> Orders { get; set; } = new();
    public List<MaintenancePlanEntity> Plans { get; set; } = new();
    public List<SparePartEntity> Spares { get; set; } = new();

    public int ActiveEquipmentCount { get; set; }
    public int OpenOrdersCount { get; set; }
    public int OverduePlansCount { get; set; }
    public int BreakdownHours { get; set; }
    public decimal SparesValue { get; set; }

    public IndexModel(
        IRepository<EquipmentEntity, Guid> equipRepo,
        IRepository<FunctionalLocationEntity, Guid> locRepo,
        IRepository<MaintenanceNotificationEntity, Guid> notifRepo,
        IRepository<MaintenanceOrderEntity, Guid> orderRepo,
        IRepository<MaintenancePlanEntity, Guid> planRepo,
        IRepository<SparePartEntity, Guid> spareRepo)
    {
        _equipRepo = equipRepo;
        _locRepo = locRepo;
        _notifRepo = notifRepo;
        _orderRepo = orderRepo;
        _planRepo = planRepo;
        _spareRepo = spareRepo;
    }

    public async Task OnGetAsync()
    {
        Equipments = await _equipRepo.GetAllAsync();
        Locations = await _locRepo.GetAllAsync();
        Notifications = await _notifRepo.GetAllAsync();
        Orders = await _orderRepo.GetAllAsync();
        Plans = await _planRepo.GetAllAsync();
        Spares = await _spareRepo.GetAllAsync();

        ActiveEquipmentCount = Equipments.Count(e => e.Status == "Operational");
        OpenOrdersCount = Orders.Count(o => o.Status != "TECO Closed" && o.Status != "Cancelled");
        OverduePlansCount = Plans.Count(p => p.Status == "Active" && p.NextDueDate.HasValue && p.NextDueDate.Value < DateTime.UtcNow);
        BreakdownHours = Notifications.Count(n => n.BreakdownFlag) * 4;
        SparesValue = Spares.Sum(s => s.IssuedQuantity * s.UnitPrice);
    }
}
