using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Admin;

public class UpdateUserDto
{
    [StringLength(200, ErrorMessage = "FullName cannot exceed 200 characters")]
    public string? FullName { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    [Phone(ErrorMessage = "PhoneNumber format is invalid")]
    [StringLength(30, ErrorMessage = "PhoneNumber cannot exceed 30 characters")]
    public string? PhoneNumber { get; set; }
}
