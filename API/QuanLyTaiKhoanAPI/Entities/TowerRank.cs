namespace QuanLyTaiKhoanAPI.Entities;

public sealed class TowerRank
{
    public string UserId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int CurrentLevel { get; set; } = 1;
    public int ConsecutiveFailures { get; set; }
    public int Epoch { get; set; }
    public int Revision { get; set; }
}
