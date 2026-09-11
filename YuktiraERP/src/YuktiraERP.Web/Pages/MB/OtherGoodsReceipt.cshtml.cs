using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class OtherGoodsReceiptModel : PageModel
{
    private readonly IInventoryMovementService _service;
    private readonly ITenantContext _tenant;

    public OtherGoodsReceiptModel(IInventoryMovementService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty]
    public int MovementType { get; set; } = 561;

    [BindProperty]
    public OtherReceiptRequest Request { get; set; } = new()
    {
        PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        Lines = new List<OtherReceiptLineRequest> { new() }
    };

    public string? Error { get; set; }
    public string? SuccessMessage { get; set; }
    public PostGoodsMovementResultDto? Result { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var dto = new PostGoodsMovementRequestDto
        {
            MovementType = MovementType,
            PostingDate = Request.PostingDate,
            DocumentDate = Request.DocumentDate,
            Reference = Request.Reference,
            Lines = Request.Lines.Select(l => new PostGoodsMovementLineDto
            {
                MaterialCode = l.MaterialCode,
                MaterialName = l.MaterialName,
                Plant = Request.Plant,
                StorageLocation = Request.StorageLocation,
                Quantity = l.Quantity,
                UOM = l.UOM,
                UnitPrice = l.UnitPrice,
                BatchNumber = l.BatchNumber
            }).ToList()
        };

        Result = await _service.PostMovementAsync(dto, _tenant.TenantId, userId);
        if (Result.Success)
            SuccessMessage = $"Document {Result.DocumentNumber} posted successfully.";
        else
            Error = string.Join("; ", Result.Errors);

        return Page();
    }

    public class OtherReceiptRequest
    {
        public string PostingDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public string DocumentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public string Plant { get; set; } = "1000";
        public string StorageLocation { get; set; } = "";
        public string? Reference { get; set; }
        public List<OtherReceiptLineRequest> Lines { get; set; } = new();
    }

    public class OtherReceiptLineRequest
    {
        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string UOM { get; set; } = "EA";
        public decimal? UnitPrice { get; set; }
        public string? BatchNumber { get; set; }
    }
}
