using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Auth;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "NewPassword is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "NewPassword must be between 6 and 100 characters")]
    public string NewPassword { get; set; } = string.Empty;
}
