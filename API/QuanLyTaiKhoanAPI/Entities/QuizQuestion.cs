using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.Entities;

public class QuizQuestion
{
    public int Id { get; set; }
    public int Grade { get; set; }

    [Required, MaxLength(30)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Explanation { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    public ICollection<ExamQuestion> Exams { get; set; } = new List<ExamQuestion>();
}
