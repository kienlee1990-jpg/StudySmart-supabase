using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuanLyTaiKhoanAPI.Controllers;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs.Quiz;
using QuanLyTaiKhoanAPI.Services;
using Xunit;

namespace QuanLyTaiKhoanAPI.Tests;

// Relational persistence regressions run offline. PostgreSQL-specific schema generation
// is checked separately; these tests do not claim to validate a live Supabase connection.
public sealed class QuizPersistenceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"hoctap-test-{Guid.NewGuid():N}.db");
    private readonly string _guestId = Guid.NewGuid().ToString("N");

    private AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={_path};Pooling=False").Options);

    private QuizController Controller(AppDbContext db, string? guestId = null)
    {
        var controller = new QuizController(db, new QuizStore(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["X-Guest-Id"] = guestId ?? _guestId;
        return controller;
    }

    [Fact]
    public async Task SessionAndAnswersSurviveNewDatabaseContexts()
    {
        int attemptId;
        List<SubmitQuizAnswerDto> answers;
        await using (var db = Open())
        {
            await db.Database.EnsureCreatedAsync();
            var store = new QuizStore(db);
            for (var i = 0; i < 10; i++)
                await store.AddQuestionAsync(5, "Toán", $"Question {i}", "Explanation",
                    [("Correct", true), ("Wrong", false)]);
            var start = Assert.IsType<OkObjectResult>(await Controller(db).StartRandomSession(new()
            {
                Grade = 5, Subject = "Toán", DurationMinutes = 15, Mode = "Test"
            }));
            attemptId = JsonSerializer.SerializeToElement(start.Value).GetProperty("attempt").GetProperty("Id").GetInt32();
            var session = await store.GetSessionAsync(attemptId);
            Assert.NotNull(session);
            Assert.Equal(10, session.Questions.Count);
            answers = session.Questions.Select(question => new SubmitQuizAnswerDto
            {
                QuestionId = question.Id, OptionId = question.Options.Single(option => option.IsCorrect).Id
            }).ToList();
            // Removing a question from the bank must preserve it for an existing attempt.
            Assert.True(await store.RemoveQuestionAsync(answers[0].QuestionId));
            Assert.Equal(9, await store.CountQuestionsAsync(5, "Toán"));
        }
        await using (var db = Open())
        {
            var session = await new QuizStore(db).GetSessionAsync(attemptId);
            Assert.NotNull(session);
            Assert.Equal(10, session.Questions.Count);
            Assert.IsType<ForbidResult>(await Controller(db, Guid.NewGuid().ToString("N"))
                .SubmitAttempt(attemptId, new() { Answers = answers }));
            Assert.IsType<OkObjectResult>(await Controller(db).SubmitAttempt(attemptId, new() { Answers = answers }));
            Assert.IsType<ConflictObjectResult>(await Controller(db).SubmitAttempt(attemptId, new() { Answers = answers }));
        }
        await using (var db = Open())
        {
            Assert.Equal(10, await db.QuizAnswers.CountAsync());
            Assert.Equal(100m, (await db.QuizAttempts.SingleAsync()).Score);
            var result = Assert.IsType<OkObjectResult>(await Controller(db).GetAttemptResult(attemptId));
            Assert.Equal(10, JsonSerializer.SerializeToElement(result.Value).GetProperty("Answers").GetArrayLength());
            Assert.IsType<NotFoundResult>(await Controller(db, Guid.NewGuid().ToString("N")).GetAttemptResult(attemptId));
        }
    }

    [Fact]
    public async Task DuplicateQuestionCheckSurvivesRestartAndFiltersByGradeAndSubject()
    {
        await using (var db = Open())
        {
            await db.Database.EnsureCreatedAsync();
            Assert.NotNull(await new QuizStore(db).AddQuestionAsync(5, "Toán", "Question", null,
                [("A", true), ("B", false)]));
        }
        await using (var db = Open())
        {
            var store = new QuizStore(db);
            Assert.Null(await store.AddQuestionAsync(5, "Toán", "QUESTION", null, [("A", true), ("B", false)]));
            Assert.Single(await store.GetQuestionsAsync(5, "Toán"));
            Assert.Empty(await store.GetQuestionsAsync(6, "Toán"));
            Assert.Empty(await store.GetQuestionsAsync(5, "Tiếng Anh"));
        }
    }

    [Fact]
    public void PostgresSchemaIncludesAllEntitiesInPrivateSchema()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        Assert.All(db.Model.GetEntityTypes(), entity => Assert.Equal("hoctap", entity.GetSchema()));
        var sql = db.Database.GenerateCreateScript();
        Assert.Contains("numeric(5,2)", sql);
        Assert.Contains("timestamp with time zone", sql);
        Assert.Contains("GENERATED BY DEFAULT AS IDENTITY", sql);
        Assert.Contains("hoctap.\"QuizAnswers\"", sql);
        Assert.DoesNotContain("AUTOINCREMENT", sql);
        var migrationSql = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("InitialSupabase", migrationSql);
        Assert.Contains("REVOKE ALL ON SCHEMA hoctap FROM PUBLIC", migrationSql);
    }

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(_path + suffix);
    }
}
