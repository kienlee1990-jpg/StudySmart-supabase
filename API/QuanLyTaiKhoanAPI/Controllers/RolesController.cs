using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.DTOs.Admin;
using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Responses;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Authorize]
[Permission(AppPermissions.ManagePermissions)]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public RolesController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _adminUserService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var role = await _adminUserService.GetRoleByIdAsync(id);
        return role == null
            ? NotFound(ApiResponse<object>.Fail("Role not found", "NOT_FOUND"))
            : Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleDto dto)
    {
        var role = await _adminUserService.CreateRoleAsync(dto.RoleName);
        var id = role.GetType().GetProperty("Id")?.GetValue(role);
        return CreatedAtAction(nameof(GetById), new { id }, role);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] RoleDto dto)
    {
        await _adminUserService.UpdateRoleAsync(id, dto.RoleName);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _adminUserService.DeleteRoleByIdAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/permissions/{permissionName}")]
    public async Task<IActionResult> AssignPermission(string id, string permissionName)
    {
        await _adminUserService.AssignPermissionToRoleAsync(id, permissionName);
        return NoContent();
    }

    [HttpDelete("{id}/permissions/{permissionName}")]
    public async Task<IActionResult> RemovePermission(string id, string permissionName)
    {
        await _adminUserService.RemovePermissionFromRoleAsync(id, permissionName);
        return NoContent();
    }
}
