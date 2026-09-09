using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class DisplayReservationModel : PageModel
{
    private readonly IStockReservationService _service;
    private readonly ITenantContext _tenant;

    public DisplayReservationModel(IStockReservationService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public string? MaterialCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Plant { get; set; }

    public List<ReservationHeaderDto>? Reservations { get; set; }
    public ReservationHeaderDto? SelectedReservation { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        Reservations = await _service.GetReservationsAsync(MaterialCode, Plant, _tenant.TenantId);
    }
}
