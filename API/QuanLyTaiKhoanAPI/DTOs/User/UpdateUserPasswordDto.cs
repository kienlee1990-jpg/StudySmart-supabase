using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.User;

public class UpdateUserPasswordDto
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "NewPassword is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "NewPassword must be between 6 and 100 characters")]
    public string NewPassword { get; set; } = string.Empty;
}
