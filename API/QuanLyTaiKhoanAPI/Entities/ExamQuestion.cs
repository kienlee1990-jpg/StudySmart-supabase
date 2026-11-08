namespace QuanLyTaiKhoanAPI.Entities;

public class ExamQuestion
{
    public int ExamId { get; set; }
    public int QuestionId { get; set; }
    public int SortOrder { get; set; }

    public QuizExam Exam { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
}
