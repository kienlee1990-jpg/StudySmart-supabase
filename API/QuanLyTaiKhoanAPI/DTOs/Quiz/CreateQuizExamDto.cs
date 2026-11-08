using System.ComponentModel.DataAnnotations;

namespace QuanLyTaiKhoanAPI.DTOs.Quiz;

public class CreateQuizExamDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(5, 12)]
    public int Grade { get; set; }

    [Required, RegularExpression("^(Toán|Tiếng Anh|Tiếng Việt)$")]
    public string Subject { get; set; } = string.Empty;

    [Required, RegularExpression("^(Study|Test|ExamRoom)$")]
    public string Mode { get; set; } = "Study";

    [Range(1, 180)]
    public int DurationMinutes { get; set; } = 30;

    [MinLength(1)]
    public List<int> QuestionIds { get; set; } = [];
}

public class SubmitQuizAttemptDto
{
    public List<SubmitQuizAnswerDto> Answers { get; set; } = [];
}

public class SubmitQuizAnswerDto
{
    public int QuestionId { get; set; }
    public int? OptionId { get; set; }
}

public class StartQuizAttemptDto
{
    [MaxLength(100)]
    public string? GuestName { get; set; }
}
