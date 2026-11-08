using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "RefreshToken is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
