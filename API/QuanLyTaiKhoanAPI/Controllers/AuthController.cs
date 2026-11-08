using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Auth;
using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Responses;
using System.Security.Claims;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message == "ACCOUNT_INACTIVE")
        {
            return StatusCode(403, ApiResponse<object>.Fail(
                "Tai khoan da bi khoa",
                "ACCOUNT_INACTIVE"));
        }
        catch (Exception ex) when (ex.Message == "ACCOUNT_LOCKED")
        {
            return StatusCode(423, ApiResponse<object>.Fail(
                "Tai khoan dang bi khoa tam thoi do dang nhap sai qua nhieu lan",
                "ACCOUNT_LOCKED"));
        }
        catch (Exception ex) when (ex.Message == "INVALID_CREDENTIALS")
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "Sai tai khoan hoac mat khau",
                "INVALID_CREDENTIALS"));
        }
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetDto dto)
    {
        await _authService.RequestPasswordResetAsync(dto);
        return Ok(new
        {
            message = "Neu email ton tai trong he thong, lien ket cap lai mat khau se duoc gui ve email."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return NoContent();
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        await _authService.LogoutAsync(User, dto.RefreshToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid token", "UNAUTHORIZED"));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user.UserId == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found", "NOT_FOUND"));
        }

        return Ok(new
        {
            user.UserId,
            user.UserName,
            user.Email,
            user.FullName,
            user.Roles,
            user.Permissions,
            user.RolePermissions
        });
    }
}
