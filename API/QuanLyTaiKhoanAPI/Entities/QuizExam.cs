using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.Entities;

public class QuizExam
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int Grade { get; set; }
    public int? TowerLevel { get; set; }

    [Required, MaxLength(30)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Mode { get; set; } = "Practice";

    public int DurationMinutes { get; set; } = 30;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
