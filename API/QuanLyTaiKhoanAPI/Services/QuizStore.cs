using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.Entities;

namespace QuanLyTaiKhoanAPI.Services;

/// <summary>Stores question banks and quiz sessions in Supabase PostgreSQL.</summary>
public sealed class QuizStore(AppDbContext db)
{
    public Task<List<QuizQuestion>> GetQuestionsAsync(int? grade = null, string? subject = null)
    {
        var query = db.QuizQuestions.AsNoTracking().Include(question => question.Options)
            .Where(question => question.IsActive);
        if (grade.HasValue) query = query.Where(question => question.Grade == grade.Value);
        if (!string.IsNullOrWhiteSpace(subject)) query = query.Where(question => question.Subject == subject);
        return query.OrderBy(question => question.Grade).ThenBy(question => question.Subject)
            .ThenBy(question => question.Id).ToListAsync();
    }

    public Task<int> CountQuestionsAsync(int grade, string subject) =>
        db.QuizQuestions.CountAsync(question => question.IsActive && question.Grade == grade && question.Subject == subject);

    public async Task<QuizQuestion?> AddQuestionAsync(int grade, string subject, string content,
        string? explanation, IEnumerable<(string Content, bool IsCorrect)> options)
    {
        if (await db.QuizQuestions.AnyAsync(question => question.IsActive && question.Grade == grade &&
            question.Subject == subject && question.Content.ToLower() == content.ToLower()))
            return null;

        var question = new QuizQuestion
        {
            Grade = grade, Subject = subject, Content = content, Explanation = explanation,
            Options = options.Select(option => new QuizOption
            {
                Content = option.Content, IsCorrect = option.IsCorrect
            }).ToList()
        };
        db.QuizQuestions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    public async Task<bool> RemoveQuestionAsync(int id)
    {
        var question = await db.QuizQuestions.FindAsync(id);
        if (question == null || !question.IsActive) return false;
        // Keep references valid for active sessions and historical answers.
        question.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<QuizSession?> GetSessionAsync(int attemptId)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().Include(item => item.Exam)
            .ThenInclude(exam => exam.Questions).ThenInclude(link => link.Question)
            .ThenInclude(question => question.Options)
            .FirstOrDefaultAsync(item => item.Id == attemptId);
        if (attempt == null || attempt.Exam.Questions.Count == 0) return null;
        return new QuizSession(attempt.Exam, attempt.Exam.Questions.OrderBy(link => link.SortOrder)
            .Select(link => link.Question).ToList());
    }
}

public sealed record QuizSession(QuizExam Exam, IReadOnlyList<QuizQuestion> Questions);
