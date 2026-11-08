using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Admin;

public class RoleDto
{
    [Required(ErrorMessage = "RoleName is required")]
    [StringLength(100, ErrorMessage = "RoleName cannot exceed 100 characters")]
    public string RoleName { get; set; } = string.Empty;
}
