using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace YuktiraERP.Web.Pages.PP;
public class MrpRunModel : PageModel
{
    [BindProperty] public string ProductName { get; set; } = "";
    [BindProperty] public int HorizonDays { get; set; } = 30;
    public string Result { get; set; } = "";
    public void OnGet() { }
    public IActionResult OnPost()
    {
        Result = $"MRP run completed for '{ProductName}' with {HorizonDays}-day horizon. Generated plans will appear shortly.";
        return Page();
    }
}
