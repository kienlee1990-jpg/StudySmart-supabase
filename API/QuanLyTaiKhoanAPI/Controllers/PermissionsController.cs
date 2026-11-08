using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.Interfaces;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Authorize]
[Permission(AppPermissions.ManagePermissions)]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public PermissionsController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await _adminUserService.GetAllPermissionsAsync();
        return Ok(permissions);
    }
}
