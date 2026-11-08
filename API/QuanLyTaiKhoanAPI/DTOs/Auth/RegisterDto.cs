using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "UserName cannot exceed 100 characters")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "FullName is required")]
    [StringLength(200, ErrorMessage = "FullName cannot exceed 200 characters")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "PhoneNumber format is invalid")]
    [StringLength(30, ErrorMessage = "PhoneNumber cannot exceed 30 characters")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }
}
