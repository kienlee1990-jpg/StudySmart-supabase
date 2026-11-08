using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Audit;

public class AuditLogQueryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 50;

    [StringLength(100, ErrorMessage = "Action cannot exceed 100 characters")]
    public string? Action { get; set; }

    [StringLength(450, ErrorMessage = "ActorUserId cannot exceed 450 characters")]
    public string? ActorUserId { get; set; }

    [StringLength(450, ErrorMessage = "TargetUserId cannot exceed 450 characters")]
    public string? TargetUserId { get; set; }

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
