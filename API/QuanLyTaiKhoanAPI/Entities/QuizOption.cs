using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.Entities;

public class QuizOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }

    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
    public QuizQuestion Question { get; set; } = null!;
}
