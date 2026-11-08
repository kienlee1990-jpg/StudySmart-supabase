namespace QuanLyTaiKhoanAPI.DTOs.Audit;

public class AuditLogEntry
{
    public string Action { get; set; } = string.Empty;
    public bool Succeeded { get; set; } = true;
    public string? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
}
