using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Controllers;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Services;
using Xunit;

namespace QuanLyTaiKhoanAPI.Tests;

public sealed class TowerQuestionPolicyTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tower-test-{Guid.NewGuid():N}.db");
    private readonly string _guestId = Guid.NewGuid().ToString("N");
    private AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={_path};Pooling=False").Options);
    private QuizController Controller(AppDbContext db)
    {
        var controller = new QuizController(db, new QuizStore(db))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Headers["X-Guest-Id"] = _guestId;
        EnsureUser(db);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, _guestId)], "Test"));
        return controller;
    }
    private static List<QuizQuestion> Bank(int grade, int normalCount = 30, int advancedCount = 30)
    {
        var result = new List<QuizQuestion>();
        foreach (var difficulty in new[] { TowerQuestionPolicy.Normal, TowerQuestionPolicy.Advanced })
        foreach (var group in new[] { 0, 1 })
        {
            var required = difficulty == TowerQuestionPolicy.Normal ? normalCount : advancedCount;
            for (int index = 0, added = 0; added < required; index++)
            {
                var content = $"Question grade {grade} {difficulty} group {group} number {index}";
                if (TowerQuestionPolicy.QuestionGroup(content) != group) continue;
                result.Add(new QuizQuestion
                {
                    Id = result.Count + 1, Grade = grade, Subject = "Toán", Difficulty = difficulty, Content = content,
                    Options = [new() { Content = "Correct", IsCorrect = true }, new() { Content = "Wrong 1" },
                        new() { Content = "Wrong 2" }, new() { Content = "Wrong 3" }]
                });
                added++;
            }
        }
        return result;
    }
    private void EnsureUser(AppDbContext db) {
        if (!db.Users.Any(user => user.Id == _guestId)) { db.Users.Add(new ApplicationUser { Id = _guestId, UserName = _guestId }); db.SaveChanges(); }
    }
    private void Unlock(AppDbContext db, int level) {
        EnsureUser(db);
        db.TowerProgress.Add(new() { UserId = _guestId, Subject = "Toán", Level = level - 1, Stars = 5 });
    }
    private static JsonElement Json(OkObjectResult response) => JsonSerializer.SerializeToElement(response.Value);
    private static int AttemptId(JsonElement response) => response.GetProperty("attempt").GetProperty("Id").GetInt32();
    private static int[] QuestionIds(JsonElement response) => response.GetProperty("quiz").GetProperty("Questions")
        .EnumerateArray().Select(question => question.GetProperty("Id").GetInt32()).ToArray();

    [Fact]
    public void AllThousandLevelsNeverReduceGradeCountOrAdvancedProportion()
    {
        var previous = TowerQuestionPolicy.GetRules(1);
        Assert.Equal((5, 5, 0), (previous.QuestionGrade, previous.RequiredQuestions, previous.AdvancedQuestions));
        for (var level = 2; level <= 1000; level++)
        {
            var current = TowerQuestionPolicy.GetRules(level);
            Assert.True(current.QuestionGrade >= previous.QuestionGrade);
            Assert.True(current.RequiredQuestions >= previous.RequiredQuestions);
            Assert.True(current.AdvancedQuestions >= previous.AdvancedQuestions);
            Assert.True(current.AdvancedQuestions * previous.RequiredQuestions >= previous.AdvancedQuestions * current.RequiredQuestions);
            Assert.InRange(current.AdvancedQuestions, 0, current.RequiredQuestions);
            previous = current;
        }
        Assert.Equal((12, 20, 20), (previous.QuestionGrade, previous.RequiredQuestions, previous.AdvancedQuestions));
        Assert.True(TowerQuestionPolicy.GetRules(100).AdvancedQuestions > 0);
    }

    [Fact]
    public void AdjacentLevelsRemainDisjointAcrossAllLevelsAndRepeatedDraws()
    {
        var bank = Enumerable.Range(5, 8).SelectMany(grade => Bank(grade)).ToList();
        var pool = new TowerQuestionPool(bank);
        var random = new Random(2026);
        for (var level = 1; level < 1000; level++)
        {
            var rules = TowerQuestionPolicy.GetRules(level);
            var selected = pool.Select(rules, random);
            var next = pool.Select(TowerQuestionPolicy.GetRules(level + 1), random);
            Assert.Equal(rules.RequiredQuestions, selected.Length);
            Assert.Equal(rules.AdvancedQuestions, selected.Count(q => q.Difficulty == TowerQuestionPolicy.Advanced));
            Assert.All(selected, q => Assert.Equal(rules.QuestionGrade, q.Grade));
            var keys = selected.Select(q => TowerQuestionPolicy.ContentKey(q.Content)).ToHashSet();
            Assert.Equal(selected.Length, keys.Count);
            Assert.DoesNotContain(next, q => keys.Contains(TowerQuestionPolicy.ContentKey(q.Content)));
            var retry = pool.Select(rules, random);
            var nextKeys = next.Select(q => TowerQuestionPolicy.ContentKey(q.Content)).ToHashSet();
            Assert.DoesNotContain(retry, q => nextKeys.Contains(TowerQuestionPolicy.ContentKey(q.Content)));
        }
    }

    [Fact]
    public void ContentGroupingIsStableAndIgnoresCaseWhitespaceAndUnicodeForm()
    {
        const string content = "Câu hỏi nâng cao";
        var variant = "  CÂU\nHỎI   NÂNG CAO  ".Normalize(System.Text.NormalizationForm.FormD);
        Assert.Equal(TowerQuestionPolicy.QuestionGroup(content), TowerQuestionPolicy.QuestionGroup(variant));
        var duplicates = Bank(5, 0, 0);
        duplicates.AddRange([
            new() { Id = 1, Grade = 5, Content = content, Difficulty = "Thường" },
            new() { Id = 2, Grade = 5, Content = variant, Difficulty = "Nâng cao" },
            new() { Id = 3, Grade = 5, Content = "Inactive question", IsActive = false }
        ]);
        var level = TowerQuestionPolicy.QuestionGroup(content) == 1 ? 1 : 2;
        var available = new TowerQuestionPool(duplicates).Availability(TowerQuestionPolicy.GetRules(level));
        Assert.Equal(0, available.NormalQuestions);
        Assert.Equal(1, available.AdvancedQuestions);
    }

    [Fact]
    public void ShufflingPreservesOptionsCorrectIdentityAndVariesCorrectPosition()
    {
        var options = new[] { new QuizOption { Id = 1, IsCorrect = true }, new QuizOption { Id = 2 },
            new QuizOption { Id = 3 }, new QuizOption { Id = 4 } };
        var positions = new HashSet<int>();
        var random = new Random(42);
        for (var trial = 0; trial < 30; trial++)
        {
            var shuffled = TowerQuestionPolicy.Shuffle(options, random);
            Assert.Equal(new[] { 1, 2, 3, 4 }, shuffled.Select(option => option.Id).Order());
            Assert.Equal(1, shuffled.Single(option => option.IsCorrect).Id);
            positions.Add(Array.FindIndex(shuffled, option => option.IsCorrect));
        }
        Assert.True(positions.Count > 1);
        Assert.Equal(1, options[0].Id);
    }

    [Fact]
    public async Task SessionAndProgressApplySameQuotasAndAvoidAdjacentQuestionsAfterRestart()
    {
        int[] firstIds;
        int[] retryIds;
        await using (var db = Open())
        {
            await db.Database.EnsureCreatedAsync();
            var bank = Bank(5);
            foreach (var question in bank) question.Id = 0;
            db.QuizQuestions.AddRange(bank);
            Unlock(db, 100); Unlock(db, 101);
            await db.SaveChangesAsync();
            var controller = Controller(db);
            var first = Json(Assert.IsType<OkObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 100 })));
            firstIds = QuestionIds(first);
            var rules = TowerQuestionPolicy.GetRules(100);
            var session = (await new QuizStore(db).GetSessionAsync(AttemptId(first)))!;
            Assert.Equal(rules.AdvancedQuestions, session.Questions.Count(q => q.Difficulty == "Nâng cao"));
            Assert.All(first.GetProperty("quiz").GetProperty("Questions").EnumerateArray(), question =>
            {
                foreach (var option in question.GetProperty("Options").EnumerateArray())
                    Assert.False(option.TryGetProperty("IsCorrect", out _));
            });
            retryIds = QuestionIds(Json(Assert.IsType<OkObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 100 }))));
            var progress = Json(Assert.IsType<OkObjectResult>(await controller.GetTowerProgress("Toán")));
            var progressLevel = progress.GetProperty("Levels")[99];
            Assert.Equal(rules.AdvancedQuestions, progressLevel.GetProperty("RequiredAdvancedQuestions").GetInt32());
            Assert.True(progressLevel.GetProperty("CanStart").GetBoolean());
        }
        await using (var db = Open())
        {
            var next = Json(Assert.IsType<OkObjectResult>(await Controller(db).StartTowerSession(new() { Subject = "Toán", Level = 101 })));
            Assert.Empty(QuestionIds(next).Intersect(firstIds));
            Assert.Empty(QuestionIds(next).Intersect(retryIds));
        }
    }

    [Fact]
    public async Task MissingAdvancedQuestionsBlocksBothProgressAndStartWithoutEasierFallback()
    {
        await using var db = Open();
        await db.Database.EnsureCreatedAsync();
        var bank = Bank(8, 40, 0);
        foreach (var question in bank) question.Id = 0;
        db.QuizQuestions.AddRange(bank);
        Unlock(db, 500);
        await db.SaveChangesAsync();
        var controller = Controller(db);
        var progress = Json(Assert.IsType<OkObjectResult>(await controller.GetTowerProgress("Toán")));
        var level = progress.GetProperty("Levels")[499];
        Assert.False(level.GetProperty("CanStart").GetBoolean());
        Assert.True(level.GetProperty("QuestionCount").GetInt32() >= level.GetProperty("RequiredQuestions").GetInt32());
        var rejected = Assert.IsType<BadRequestObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 500 }));
        Assert.Contains("Nâng cao", JsonSerializer.SerializeToElement(rejected.Value).GetProperty("message").GetString());
        Assert.Equal(0, await db.QuizAttempts.CountAsync());
    }

    [Fact]
    public async Task LastLevelUsesOnlyAdvancedAndMissingPreviousProgressDoesNotUnlockLevelTwo()
    {
        await using var db = Open();
        await db.Database.EnsureCreatedAsync();
        var bank = Bank(12, 0, 25);
        foreach (var question in bank) question.Id = 0;
        db.QuizQuestions.AddRange(bank);
        Unlock(db, 1000);
        await db.SaveChangesAsync();
        var controller = Controller(db);
        Assert.IsType<ConflictObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 2 }));
        var response = Json(Assert.IsType<OkObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 1000 })));
        var session = (await new QuizStore(db).GetSessionAsync(AttemptId(response)))!;
        Assert.Equal(20, session.Questions.Count);
        Assert.All(session.Questions, question => Assert.Equal("Nâng cao", question.Difficulty));
    }

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(_path + suffix);
    }
}
