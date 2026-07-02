using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService) => _authService = authService;

    [BindProperty] public LoginInput Input { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var device = Request.Headers["User-Agent"].ToString();
            var request = new LoginRequest
            {
                ClientNumber = Input.ClientNumber,
                UserId = Input.UserId,
                Password = Input.Password,
                Language = Input.Language,
                System = Input.System
            };

            var result = await _authService.LoginAsync(request, ip, device);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserProfile.UserId.ToString()),
                new(ClaimTypes.Name, result.UserProfile.Username),
                new(ClaimTypes.GivenName, result.UserProfile.FullName),
                new(ClaimTypes.Role, result.UserProfile.Role),
                new("TenantId", result.UserProfile.TenantId?.ToString() ?? ""),
                new("IsSuperUser", result.UserProfile.IsSuperUser.ToString().ToLower()),
                new("AccessToken", result.AccessToken)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToPage("/Dashboard/Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch
        {
            ErrorMessage = "Invalid credentials. Please try again.";
            return Page();
        }
    }

    public class LoginInput
    {
        public string ClientNumber { get; set; } = "1000";
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Language { get; set; } = "EN";
        public string System { get; set; } = "DEV";
    }
}
