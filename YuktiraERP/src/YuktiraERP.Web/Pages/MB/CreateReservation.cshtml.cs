using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class CreateReservationModel : PageModel
{
    private readonly IStockReservationService _service;
    private readonly ITenantContext _tenant;

    public CreateReservationModel(IStockReservationService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty]
    public CreateReservationForm Reservation { get; set; } = new()
    {
        RequirementDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
        Lines = new List<CreateReservationLineForm> { new() }
    };

    public string? Error { get; set; }
    public string? SuccessMessage { get; set; }
    public ReservationResultDto? Result { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var dto = new CreateReservationDto
        {
            RequirementDate = Reservation.RequirementDate,
            CostCenter = Reservation.CostCenter,
            OrderNumber = Reservation.OrderNumber,
            HeaderText = Reservation.HeaderText,
            ReservationType = MbReservationType.Manual,
            Lines = Reservation.Lines.Select(l => new CreateReservationLineDto
            {
                MaterialCode = l.MaterialCode,
                MaterialName = l.MaterialName,
                Plant = l.Plant,
                StorageLocation = l.StorageLocation,
                BatchNumber = l.BatchNumber,
                RequiredQuantity = l.RequiredQuantity,
                UOM = l.UOM
            }).ToList()
        };

        Result = await _service.CreateReservationAsync(dto, _tenant.TenantId, userId);
        if (Result.Success)
            SuccessMessage = $"Reservation {Result.ReservationNumber} created.";
        else
            Error = string.Join("; ", Result.Errors);

        return Page();
    }

    public class CreateReservationForm
    {
        public string RequirementDate { get; set; } = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
        public string? CostCenter { get; set; }
        public string? OrderNumber { get; set; }
        public string? HeaderText { get; set; }
        public List<CreateReservationLineForm> Lines { get; set; } = new();
    }

    public class CreateReservationLineForm
    {
        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Plant { get; set; } = "1000";
        public string StorageLocation { get; set; } = "";
        public string? BatchNumber { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string UOM { get; set; } = "EA";
    }
}
