using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyTaiKhoanAPI.DTOs.Auth;
using QuanLyTaiKhoanAPI.Entities;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth/register-student")]
public class StudentRegistrationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public StudentRegistrationController(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterStudentDto dto)
    {
        var email = dto.Email.Trim();
        if (await _users.FindByEmailAsync(email) != null)
            return Conflict(new { message = "Email này đã được sử dụng." });

        const string roleName = "Student";
        if (!await _roles.RoleExistsAsync(roleName))
        {
            var roleResult = await _roles.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
                return Problem("Không thể tạo vai trò học sinh.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            LockoutEnabled = true
        };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(error => error.Description)) });

        var assignRole = await _users.AddToRoleAsync(user, roleName);
        if (!assignRole.Succeeded)
        {
            await _users.DeleteAsync(user);
            return Problem("Không thể gán vai trò học sinh.");
        }

        return Created("", new { user.Id, user.Email, user.FullName });
    }
}
