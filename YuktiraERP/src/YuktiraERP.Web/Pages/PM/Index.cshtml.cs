using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM;

public class IndexModel : PageModel
{
    private readonly IRepository<EquipmentEntity, Guid> _equipRepo;
    private readonly IRepository<MaintenancePlanEntity, Guid> _planRepo;
    private readonly IRepository<MaintenanceOrderEntity, Guid> _orderRepo;

    public List<EquipmentEntity> Equipments { get; set; } = new();
    public List<MaintenancePlanEntity> Plans { get; set; } = new();
    public List<MaintenanceOrderEntity> Orders { get; set; } = new();

    public IndexModel(
        IRepository<EquipmentEntity, Guid> equipRepo,
        IRepository<MaintenancePlanEntity, Guid> planRepo,
        IRepository<MaintenanceOrderEntity, Guid> orderRepo)
    {
        _equipRepo = equipRepo;
        _planRepo = planRepo;
        _orderRepo = orderRepo;
    }

    public async Task OnGetAsync()
    {
        Equipments = await _equipRepo.GetAllAsync();
        Plans = await _planRepo.GetAllAsync();
        Orders = await _orderRepo.GetAllAsync();
    }
}
