using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email or username is required")]
    [StringLength(256, ErrorMessage = "Email or username cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; } = string.Empty;
}
