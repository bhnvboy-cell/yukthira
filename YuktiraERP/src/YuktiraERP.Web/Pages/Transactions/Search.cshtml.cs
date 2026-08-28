using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

public class SearchModel : PageModel
{
    private readonly ITransactionCodeService _service;
    public SearchModel(ITransactionCodeService service) => _service = service;

    public async Task<IActionResult> OnGetAsync([FromQuery] string q)
    {
        var results = await _service.SearchAsync(q ?? "");
        Response.ContentType = "application/json";
        return new ContentResult
        {
            Content = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }),
            ContentType = "application/json",
            StatusCode = 200
        };
    }
}
