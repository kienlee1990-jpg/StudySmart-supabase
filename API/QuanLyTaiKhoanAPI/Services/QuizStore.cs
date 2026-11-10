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

    public async Task<(int Questions, int Options, int ExamLinks, int Answers)> DeleteAllQuestionsAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var answers = await db.QuizAnswers.ExecuteDeleteAsync();
        var examLinks = await db.ExamQuestions.ExecuteDeleteAsync();
        var options = await db.QuizOptions.ExecuteDeleteAsync();
        var questions = await db.QuizQuestions.ExecuteDeleteAsync();
        await transaction.CommitAsync();
        return (questions, options, examLinks, answers);
    }

    public async Task<QuizQuestion?> AddQuestionAsync(int grade, string subject, string content,
        string? explanation, IEnumerable<(string Content, bool IsCorrect)> options, string difficulty = "Thường", string? topic = null)
    {
        if (await db.QuizQuestions.AnyAsync(question => question.IsActive && question.Grade == grade &&
            question.Subject == subject && question.Content.ToLower() == content.ToLower()))
            return null;

        var question = new QuizQuestion
        {
            Grade = grade, Subject = subject, Difficulty = difficulty, Topic = topic, Content = content, Explanation = explanation,
            Options = options.Select(option => new QuizOption
            {
                Content = option.Content, IsCorrect = option.IsCorrect
            }).ToList()
        };
        db.QuizQuestions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    public async Task<(int Imported, int Skipped)> AddQuestionsAsync(IReadOnlyCollection<ImportedQuestionRow> rows)
    {
        if (rows.Count == 0) return (0, 0);

        var grades = rows.Select(row => row.Grade).Distinct().ToArray();
        var subjects = rows.Select(row => row.Subject).Distinct().ToArray();
        var existing = await db.QuizQuestions.AsNoTracking()
            .Where(question => question.IsActive && grades.Contains(question.Grade) && subjects.Contains(question.Subject))
            .Select(question => new { question.Grade, question.Subject, question.Content })
            .ToListAsync();

        static string Key(int grade, string subject, string content) =>
            $"{grade}\u001f{subject}\u001f{content}";

        var known = new HashSet<string>(existing.Select(question => Key(question.Grade, question.Subject, question.Content)),
            StringComparer.OrdinalIgnoreCase);
        var additions = new List<QuizQuestion>();
        var skipped = 0;
        foreach (var row in rows)
        {
            if (!known.Add(Key(row.Grade, row.Subject, row.Content)))
            {
                skipped++;
                continue;
            }

            additions.Add(new QuizQuestion
            {
                Grade = row.Grade,
                Subject = row.Subject,
                Difficulty = row.Difficulty,
                Topic = row.Topic,
                Content = row.Content,
                Explanation = row.Explanation,
                Options = row.Options.Select((content, index) => new QuizOption
                {
                    Content = content,
                    IsCorrect = index == row.CorrectIndex
                }).ToList()
            });
        }

        if (additions.Count > 0)
        {
            db.QuizQuestions.AddRange(additions);
            await db.SaveChangesAsync();
        }

        return (additions.Count, skipped);
    }

    public async Task<bool> SetQuestionDifficultyAsync(int id, string difficulty)
    {
        var question = await db.QuizQuestions.FindAsync(id);
        if (question == null || !question.IsActive) return false;
        question.Difficulty = difficulty;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetQuestionTopicAsync(int id, string? topic)
    {
        var question = await db.QuizQuestions.FindAsync(id);
        if (question == null || !question.IsActive) return false;
        question.Topic = topic;
        await db.SaveChangesAsync();
        return true;
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
