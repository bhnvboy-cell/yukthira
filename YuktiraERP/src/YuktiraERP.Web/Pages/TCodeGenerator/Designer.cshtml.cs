using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.TCodeGenerator;

public class DesignerModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string Id { get; set; } = "";
    public string Code { get; set; } = "";

    public async Task OnGetAsync()
    {
        if (string.IsNullOrEmpty(Id)) return;
        using var http = new HttpClient();
        var res = await http.GetAsync($"{Request.Scheme}://{Request.Host}/api/tcode-generator/definitions/{Id}");
        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Code = doc.RootElement.GetProperty("code").GetString() ?? "";
        }
    }
}
