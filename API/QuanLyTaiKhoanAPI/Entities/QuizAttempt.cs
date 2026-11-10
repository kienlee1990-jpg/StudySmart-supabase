namespace QuanLyTaiKhoanAPI.Entities;

public class QuizAttempt
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string? UserId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public int? TowerRankAtStart { get; set; }
    public int? TowerRankEpochAtStart { get; set; }
    public string? TowerRankResult { get; set; }
    public int TotalQuestions { get; set; }

    public QuizExam Exam { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}
