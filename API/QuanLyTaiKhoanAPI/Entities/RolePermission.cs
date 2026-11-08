using Microsoft.AspNetCore.Identity;

namespace QuanLyTaiKhoanAPI.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = null!;
    public IdentityRole Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
