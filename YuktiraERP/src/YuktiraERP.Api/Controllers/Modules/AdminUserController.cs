using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOrAbove")]
public class AdminUserController : ControllerBase
{
    private readonly IAdminUserService _users;

    public AdminUserController(IAdminUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.GetAllAsync();
        return Ok(new { data = users, count = users.Count });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        return user == null ? NotFound(new { error = "User not found" }) : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminUserCreateRequest request)
    {
        var (ok, error, user) = await _users.CreateAsync(request);
        if (!ok)
            return BadRequest(new { error });
        return CreatedAtAction(nameof(Get), new { id = user!.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminUserUpdateRequest request)
    {
        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        var (ok, error, user) = await _users.UpdateAsync(id, request, actor);
        if (!ok)
            return error == "User not found" ? NotFound(new { error }) : BadRequest(new { error });
        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        var (ok, error) = await _users.SetActiveAsync(id, false, actor);
        if (!ok)
            return BadRequest(new { error });
        return Ok(new { success = true });
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        var (ok, error) = await _users.SetActiveAsync(id, true, actor);
        if (!ok)
            return BadRequest(new { error });
        return Ok(new { success = true });
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var (ok, error) = await _users.UnlockAsync(id);
        if (!ok)
            return BadRequest(new { error });
        return Ok(new { success = true });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        var (ok, error) = await _users.ResetPasswordAsync(id, request.Password);
        if (!ok)
            return BadRequest(new { error });
        return Ok(new { success = true });
    }
}

public class ResetPasswordRequest
{
    public string Password { get; set; } = "";
}
