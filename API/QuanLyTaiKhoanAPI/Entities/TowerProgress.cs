namespace QuanLyTaiKhoanAPI.Entities;

public sealed class TowerProgress
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Stars { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
