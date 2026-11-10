using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Quiz;

public class CreateQuizQuestionDto
{
    [Range(5, 12)]
    public int Grade { get; set; }

    [Required, RegularExpression("^(Toán|Tiếng Anh|Tiếng Việt)$")]
    public string Subject { get; set; } = string.Empty;

    [RegularExpression("^(Thường|Nâng cao)$")]
    public string Difficulty { get; set; } = "Thường";

    [MaxLength(200)]
    public string? Topic { get; set; }

    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Explanation { get; set; }

    [MinLength(2), MaxLength(6)]
    public List<CreateQuizOptionDto> Options { get; set; } = [];
}

public class CreateQuizOptionDto
{
    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
