using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Admin;

namespace QuanLyTaiKhoanAPI.Interfaces;

public interface IAdminUserService
{
    Task<IEnumerable<object>> GetAllUsersAsync();
    Task<object?> GetUserByIdAsync(string id);
    Task<object> CreateUserAsync(RegisterDto dto);
    Task UpdateUserAsync(string id, UpdateUserDto dto);
    Task AssignRoleAsync(string userId, string roleName);
    Task RemoveRoleAsync(string userId, string roleName);
    Task AssignPermissionAsync(string userId, string permissionName);
    Task RemovePermissionAsync(string userId, string permissionName);
    Task AssignPermissionToRoleAsync(string roleId, string permissionName);
    Task RemovePermissionFromRoleAsync(string roleId, string permissionName);
    Task<IEnumerable<object>> GetAllRolesAsync();
    Task<object?> GetRoleByIdAsync(string roleId);
    Task<object> CreateRoleAsync(string roleName);
    Task UpdateRoleAsync(string roleId, string roleName);
    Task DeleteRoleByIdAsync(string roleId);
    Task<IEnumerable<string>> GetAllPermissionsAsync();
}
