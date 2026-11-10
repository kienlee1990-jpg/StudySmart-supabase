using System.Text.Json;
using System.Security.Claims;
using QuanLyTaiKhoanAPI.Entities;
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

    private QuizController Controller(AppDbContext db, string? guestId = null, bool authenticated = false)
    {
        var controller = new QuizController(db, new QuizStore(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["X-Guest-Id"] = guestId ?? _guestId;
        if (authenticated) {
            var id = guestId ?? _guestId;
            if (!db.Users.Any(user => user.Id == id)) { db.Users.Add(new ApplicationUser { Id = id, UserName = id }); db.SaveChanges(); }
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], "Test"));
        }
        return controller;
    }

    [Fact]
    public async Task GuestsCannotStartTowerOrExamRoomEvenWithGuestIdentity()
    {
        await using var db = Open();
        await db.Database.EnsureCreatedAsync();
        var controller = Controller(db);
        Assert.IsType<UnauthorizedResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 1 }));
        Assert.IsType<UnauthorizedResult>(await controller.StartRandomSession(new() { Subject = "Toán", Grade = 5, Mode = "ExamRoom", DurationMinutes = 60 }));
        Assert.Empty(await db.QuizAttempts.ToListAsync());
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
    public async Task TowerAnswersAreSequentialImmutableAndUsedForFinalScore()
    {
        int attemptId;
        List<SubmitQuizAnswerDto> correctAnswers;
        await using (var db = Open())
        {
            await db.Database.EnsureCreatedAsync();
            var store = new QuizStore(db);
            for (var i = 0; i < 40; i++)
                await store.AddQuestionAsync(5, "Toán", $"Tower question {i}", "Explanation",
                    [("Correct", true), ("Wrong", false)]);
            var controller = Controller(db, authenticated: true);
            var start = Assert.IsType<OkObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 1 }));
            attemptId = JsonSerializer.SerializeToElement(start.Value).GetProperty("attempt").GetProperty("Id").GetInt32();
            var session = (await store.GetSessionAsync(attemptId))!;
            correctAnswers = session.Questions.Select(question => new SubmitQuizAnswerDto
            {
                QuestionId = question.Id, OptionId = question.Options.Single(option => option.IsCorrect).Id
            }).ToList();
            Assert.IsType<ConflictObjectResult>(await controller.CheckStudyAnswer(attemptId, correctAnswers[1]));
            Assert.IsType<ForbidResult>(await Controller(db, Guid.NewGuid().ToString("N"), authenticated: true)
                .CheckStudyAnswer(attemptId, correctAnswers[0]));
            var timeout = new SubmitQuizAnswerDto { QuestionId = correctAnswers[0].QuestionId, OptionId = null };
            var response = Assert.IsType<OkObjectResult>(await controller.CheckStudyAnswer(attemptId, timeout));
            var feedback = JsonSerializer.SerializeToElement(response.Value);
            Assert.False(feedback.GetProperty("IsCorrect").GetBoolean());
            Assert.Equal(correctAnswers[0].OptionId, feedback.GetProperty("CorrectOptionId").GetInt32());
            Assert.IsType<OkObjectResult>(await controller.CheckStudyAnswer(attemptId, timeout));
            Assert.IsType<ConflictObjectResult>(await controller.CheckStudyAnswer(attemptId, correctAnswers[0]));
            Assert.IsType<ConflictObjectResult>(await controller.SubmitAttempt(attemptId, new() { Answers = correctAnswers }));
        }
        // Locked answers must survive a new request/database context.
        await using (var db = Open())
        {
            var controller = Controller(db, authenticated: true);
            foreach (var answer in correctAnswers.Skip(1))
                Assert.IsType<OkObjectResult>(await controller.CheckStudyAnswer(attemptId, answer));
            Assert.IsType<ConflictObjectResult>(await controller.CheckStudyAnswer(attemptId,
                new() { QuestionId = correctAnswers[0].QuestionId, OptionId = null }));
            // A forged all-correct submission cannot replace the timed-out answer.
            var submitted = Assert.IsType<OkObjectResult>(await controller.SubmitAttempt(attemptId, new() { Answers = correctAnswers }));
            var result = JsonSerializer.SerializeToElement(submitted.Value);
            Assert.Equal(80m, result.GetProperty("Score").GetDecimal());
            Assert.Equal(4, result.GetProperty("CorrectCount").GetInt32());
            Assert.Empty(result.GetProperty("Answers").EnumerateArray());
            Assert.Equal(5, await db.QuizAnswers.CountAsync());
            var savedResult = Assert.IsType<OkObjectResult>(await controller.GetAttemptResult(attemptId));
            Assert.Empty(JsonSerializer.SerializeToElement(savedResult.Value).GetProperty("Answers").EnumerateArray());
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
