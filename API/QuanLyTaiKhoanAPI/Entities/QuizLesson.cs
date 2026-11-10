using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.Entities;

public class QuizLesson
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Topic { get; set; }

    [MaxLength(2000)]
    public string? VideoUrl { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    [Required, MaxLength(20000)]
    public string Content { get; set; } = string.Empty;

    public int Grade { get; set; }

    [Required, MaxLength(30)]
    public string Subject { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
