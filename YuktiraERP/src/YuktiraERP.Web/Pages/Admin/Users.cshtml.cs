using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Web.Pages.Admin;

[Authorize(Policy = "AdminOrAbove")]
public class UsersModel : PageModel
{
    public static readonly string[] Roles = AdminUserService.AllowedRoles;

    private readonly IAdminUserService _users;

    public UsersModel(IAdminUserService users) => _users = users;

    public List<AdminUserDto> Users { get; set; } = new();

    [BindProperty] public CreateInput Input { get; set; } = new();
    [BindProperty] public Guid? UpdateId { get; set; }
    [BindProperty] public string? UpdateRole { get; set; }
    [BindProperty] public string? UpdateEmail { get; set; }
    [BindProperty] public bool UpdateActive { get; set; }
    [BindProperty] public Guid? DeactivateId { get; set; }
    [BindProperty] public Guid? ActivateId { get; set; }
    [BindProperty] public Guid? UnlockId { get; set; }
    [BindProperty] public Guid? ResetId { get; set; }
    [BindProperty] public string? NewPassword { get; set; }

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public async Task OnGetAsync()
    {
        Users = await _users.GetAllAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var (ok, error, _) = await _users.CreateAsync(new AdminUserCreateRequest
        {
            UserId = Input.UserId,
            Email = Input.Email,
            Role = Input.Role,
            Password = Input.Password
        });
        Message = ok ? $"User '{Input.UserId}' created." : error;
        IsError = !ok;
        return await ReloadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (UpdateId.HasValue)
        {
            var actor = User.Identity?.Name ?? "";
            var (ok, error, _) = await _users.UpdateAsync(UpdateId.Value, new AdminUserUpdateRequest
            {
                Email = UpdateEmail ?? "",
                Role = UpdateRole ?? "READ_ONLY",
                IsActive = UpdateActive
            }, actor);
            Message = ok ? "User updated." : error;
            IsError = !ok;
        }
        return await ReloadAsync();
    }

    public async Task<IActionResult> OnPostDeactivateAsync()
    {
        if (DeactivateId.HasValue)
        {
            var actor = User.Identity?.Name ?? "";
            var (ok, error) = await _users.SetActiveAsync(DeactivateId.Value, false, actor);
            Message = ok ? "User deactivated." : error;
            IsError = !ok;
        }
        return await ReloadAsync();
    }

    public async Task<IActionResult> OnPostActivateAsync()
    {
        if (ActivateId.HasValue)
        {
            var actor = User.Identity?.Name ?? "";
            var (ok, error) = await _users.SetActiveAsync(ActivateId.Value, true, actor);
            Message = ok ? "User activated." : error;
            IsError = !ok;
        }
        return await ReloadAsync();
    }

    public async Task<IActionResult> OnPostUnlockAsync()
    {
        if (UnlockId.HasValue)
        {
            var (ok, error) = await _users.UnlockAsync(UnlockId.Value);
            Message = ok ? "Account unlocked." : error;
            IsError = !ok;
        }
        return await ReloadAsync();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (ResetId.HasValue)
        {
            var (ok, error) = await _users.ResetPasswordAsync(ResetId.Value, NewPassword ?? "");
            Message = ok ? "Password reset." : error;
            IsError = !ok;
        }
        return await ReloadAsync();
    }

    private async Task<IActionResult> ReloadAsync()
    {
        Users = await _users.GetAllAsync();
        return Page();
    }

    public class CreateInput
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "READ_ONLY";
        public string Password { get; set; } = "";
    }
}
