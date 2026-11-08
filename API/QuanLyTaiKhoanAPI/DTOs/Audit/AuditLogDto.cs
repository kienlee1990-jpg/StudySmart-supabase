namespace QuanLyTaiKhoanAPI.DTOs.Audit;

public class AuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
