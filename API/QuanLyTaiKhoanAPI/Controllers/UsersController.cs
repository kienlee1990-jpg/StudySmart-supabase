using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.DTOs.Admin;
using QuanLyTaiKhoanAPI.DTOs.User;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Middlewares;
using QuanLyTaiKhoanAPI.Responses;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public UsersController(
        IAdminUserService adminUserService,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService)
    {
        _adminUserService = adminUserService;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Permission(AppPermissions.ManageUsers)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _adminUserService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Permission(AppPermissions.ManageUsers)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _adminUserService.GetUserByIdAsync(id);
        return user == null
            ? NotFound(ApiResponse<object>.Fail("User not found", "NOT_FOUND"))
            : Ok(user);
    }

    [HttpPost]
    [Permission(AppPermissions.ManageUsers)]
    public async Task<IActionResult> Create([FromBody] RegisterDto dto)
    {
        var user = await _adminUserService.CreateUserAsync(dto);
        var id = user.GetType().GetProperty("Id")?.GetValue(user);
        return CreatedAtAction(nameof(GetById), new { id }, user);
    }

    [HttpPut("{id}")]
    [Permission(AppPermissions.ManageUsers)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        await _adminUserService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    [HttpPut("{id}/roles/{roleName}")]
    [Permission(AppPermissions.ManagePermissions)]
    public async Task<IActionResult> AssignRole(string id, string roleName)
    {
        await _adminUserService.AssignRoleAsync(id, roleName);
        return NoContent();
    }

    [HttpDelete("{id}/roles/{roleName}")]
    [Permission(AppPermissions.ManagePermissions)]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        await _adminUserService.RemoveRoleAsync(id, roleName);
        return NoContent();
    }

    [HttpPut("{id}/permissions/{permissionName}")]
    [Permission(AppPermissions.ManagePermissions)]
    public async Task<IActionResult> AssignPermission(string id, string permissionName)
    {
        await _adminUserService.AssignPermissionAsync(id, permissionName);
        return NoContent();
    }

    [HttpDelete("{id}/permissions/{permissionName}")]
    [Permission(AppPermissions.ManagePermissions)]
    public async Task<IActionResult> RemovePermission(string id, string permissionName)
    {
        await _adminUserService.RemovePermissionAsync(id, permissionName);
        return NoContent();
    }

    [HttpPost("{id}/password-reset-tokens")]
    [Permission(AppPermissions.ResetUserPasswords)]
    public async Task<IActionResult> CreatePasswordResetToken(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new NotFoundException("User not found");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "AdminPasswordResetTokenCreated",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "User",
            EntityId = user.Id
        });

        return Created(string.Empty, new
        {
            UserId = user.Id,
            user.Email,
            Token = Uri.EscapeDataString(token)
        });
    }

    [HttpPut("{id}/password")]
    [Permission(AppPermissions.ResetUserPasswords)]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] UpdateUserPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            throw new BadRequestException("Token is required");
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            throw new BadRequestException("NewPassword is required");
        }

        var user = await _userManager.FindByIdAsync(id)
            ?? throw new NotFoundException("User not found");

        var decodedToken = Uri.UnescapeDataString(dto.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "AdminPasswordChanged",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "User",
            EntityId = user.Id
        });

        return NoContent();
    }
}
