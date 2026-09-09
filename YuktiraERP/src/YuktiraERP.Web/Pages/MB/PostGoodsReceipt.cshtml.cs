using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class PostGoodsReceiptModel : PageModel
{
    private readonly IInventoryMovementService _service;
    private readonly ITenantContext _tenant;

    public PostGoodsReceiptModel(IInventoryMovementService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty]
    public PostGoodsMovementRequest Request { get; set; } = new()
    {
        PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        Lines = new List<PostGoodsMovementLineRequest> { new() }
    };

    public string? Error { get; set; }
    public string? SuccessMessage { get; set; }
    public PostGoodsMovementResultDto? Result { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var dto = new PostGoodsMovementRequestDto
        {
            MovementType = 101,
            PostingDate = Request.PostingDate,
            DocumentDate = Request.DocumentDate,
            HeaderText = Request.HeaderText,
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
                BatchNumber = l.BatchNumber,
                PurchaseOrderNo = l.PurchaseOrderNo
            }).ToList()
        };

        Result = await _service.PostGoodsReceiptAsync(dto, _tenant.TenantId, userId);
        if (Result.Success)
            SuccessMessage = $"Document {Result.DocumentNumber} posted successfully.";
        else
            Error = string.Join("; ", Result.Errors);

        return Page();
    }

    public class PostGoodsMovementRequest
    {
        public string PostingDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public string DocumentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public string? HeaderText { get; set; }
        public string? Reference { get; set; }
        public string Plant { get; set; } = "1000";
        public string StorageLocation { get; set; } = "";
        public List<PostGoodsMovementLineRequest> Lines { get; set; } = new();
    }

    public class PostGoodsMovementLineRequest
    {
        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string UOM { get; set; } = "EA";
        public decimal? UnitPrice { get; set; }
        public string? BatchNumber { get; set; }
        public string? PurchaseOrderNo { get; set; }
    }
}
