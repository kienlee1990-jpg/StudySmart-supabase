using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Auth;
using System.Security.Claims;

namespace QuanLyTaiKhoanAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task RequestPasswordResetAsync(RequestPasswordResetDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(ClaimsPrincipal user, string refreshToken);
    Task<(string UserId, string UserName, string Email, string FullName, List<string> Roles, List<string> Permissions, List<string> RolePermissions)> GetCurrentUserAsync(string userId);
}
