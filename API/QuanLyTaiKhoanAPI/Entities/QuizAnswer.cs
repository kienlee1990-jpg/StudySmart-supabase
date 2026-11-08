namespace QuanLyTaiKhoanAPI.Entities;

public class QuizAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? OptionId { get; set; }
    public bool IsCorrect { get; set; }

    public QuizAttempt Attempt { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
    public QuizOption? Option { get; set; }
}
