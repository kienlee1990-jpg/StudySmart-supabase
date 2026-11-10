using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Controllers;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs.Quiz;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Services;
using Xunit;

namespace QuanLyTaiKhoanAPI.Tests;

public sealed class TowerRankTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"rank-{Guid.NewGuid():N}.db");
    private const string UserId = "rank-player";
    private AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options);
    private static QuizController Controller(AppDbContext db) => new(db, new QuizStore(db)) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "Test"))
        } }
    };
    private async Task Seed()
    {
        await using var db = Open();
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { Id = UserId, UserName = UserId });
        for (var level = 1; level < 4; level++) db.TowerProgress.Add(new() { UserId = UserId, Subject = "Toán", Level = level, Stars = 5 });
        await db.SaveChangesAsync();
        var store = new QuizStore(db);
        for (var i = 0; i < 80; i++) await store.AddQuestionAsync(5, "Toán", $"Rank question {i}", null, [("Correct", true), ("Wrong", false)]);
    }
    private async Task<int> Start(int level)
    {
        await using var db = Open();
        var started = Assert.IsType<OkObjectResult>(await Controller(db).StartTowerSession(new() { Subject = "Toán", Level = level }));
        return JsonSerializer.SerializeToElement(started.Value).GetProperty("attempt").GetProperty("Id").GetInt32();
    }
    private async Task<JsonElement> Finish(int id, bool passed, int correctAnswers = 0)
    {
        await using var db = Open();
        var controller = Controller(db);
        var session = (await new QuizStore(db).GetSessionAsync(id))!;
        var answers = session.Questions.Select((question, index) => new SubmitQuizAnswerDto {
            QuestionId = question.Id, OptionId = passed || index < correctAnswers ? question.Options.Single(option => option.IsCorrect).Id : null
        }).ToList();
        foreach (var answer in answers) Assert.IsType<OkObjectResult>(await controller.CheckStudyAnswer(id, answer));
        var result = Assert.IsType<OkObjectResult>(await controller.SubmitAttempt(id, new() { Answers = answers }));
        return JsonSerializer.SerializeToElement(result.Value).GetProperty("Tower");
    }
    private async Task<JsonElement> Play(int level, bool passed = false) => await Finish(await Start(level), passed);

    [Fact]
    public async Task RepeatedDemotionsSurviveReloadKeepRecordsAndDoNotCountReplays()
    {
        await Seed();
        Assert.Equal(1, (await Play(4)).GetProperty("ConsecutiveFailures").GetInt32());
        Assert.Equal(2, (await Play(4)).GetProperty("ConsecutiveFailures").GetInt32());
        var replay = await Play(2);
        Assert.False(replay.GetProperty("CountsForRank").GetBoolean());
        Assert.Equal(2, replay.GetProperty("ConsecutiveFailures").GetInt32());
        var third = await Play(4);
        Assert.True(third.GetProperty("Demoted").GetBoolean());
        Assert.Equal(3, third.GetProperty("CurrentLevel").GetInt32());
        await Play(3); await Play(3);
        Assert.Equal(2, (await Play(3)).GetProperty("CurrentLevel").GetInt32());
        await Play(2); await Play(2);
        Assert.Equal(1, (await Play(2)).GetProperty("CurrentLevel").GetInt32());
        await Play(1); await Play(1);
        var floor = await Play(1);
        Assert.Equal(1, floor.GetProperty("CurrentLevel").GetInt32());
        Assert.False(floor.GetProperty("Demoted").GetBoolean());
        await using var db = Open();
        var controller = Controller(db);
        var progress = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.GetTowerProgress("Toán")).Value);
        Assert.Equal(1, progress.GetProperty("CurrentLevel").GetInt32());
        Assert.False(progress.GetProperty("Levels")[1].GetProperty("IsUnlocked").GetBoolean());
        Assert.Equal(5, progress.GetProperty("Levels")[1].GetProperty("Stars").GetInt32());
        Assert.IsType<ConflictObjectResult>(await controller.StartTowerSession(new() { Subject = "Toán", Level = 4 }));
    }

    [Fact]
    public async Task SuccessResetsFailuresAndCanReopenPreviouslyCompletedRank()
    {
        await Seed();
        await Play(4); await Play(4);
        var success = await Play(4, true);
        Assert.Equal(5, success.GetProperty("CurrentLevel").GetInt32());
        Assert.Equal(0, success.GetProperty("ConsecutiveFailures").GetInt32());
        await Play(5); await Play(5); await Play(5);
        var recovered = await Play(4, true);
        Assert.Equal(5, recovered.GetProperty("CurrentLevel").GetInt32());
        Assert.False(recovered.GetProperty("EarnedStar").GetBoolean());
        Assert.True(recovered.GetProperty("Passed").GetBoolean());
    }

    [Fact]
    public async Task OldSessionAndDoubleSubmissionCannotAffectNewRankAndResultPersists()
    {
        await Seed();
        var stale = await Start(4);
        await Play(4); await Play(4);
        var finalId = await Start(4);
        await Finish(finalId, false);
        var old = await Finish(stale, true);
        Assert.False(old.GetProperty("CountsForRank").GetBoolean());
        Assert.Equal(3, old.GetProperty("CurrentLevel").GetInt32());
        await using var db = Open();
        var controller = Controller(db);
        Assert.IsType<ConflictObjectResult>(await controller.SubmitAttempt(finalId, new()));
        var saved = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.GetAttemptResult(finalId)).Value);
        Assert.True(saved.GetProperty("Tower").GetProperty("Demoted").GetBoolean());
        Assert.Equal(3, saved.GetProperty("Tower").GetProperty("CurrentLevel").GetInt32());
        Assert.Equal(3, (await db.TowerRanks.SingleAsync()).CurrentLevel);
    }

    [Fact]
    public async Task FourStarsStillCountsAsFailureAndSubjectsHaveSeparateRanks()
    {
        await Seed();
        var result = await Finish(await Start(4), false, 4);
        Assert.Equal(4, result.GetProperty("AttemptStars").GetInt32());
        Assert.False(result.GetProperty("Passed").GetBoolean());
        Assert.Equal(1, result.GetProperty("ConsecutiveFailures").GetInt32());
        await using var db = Open();
        var english = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await Controller(db).GetTowerProgress("Tiếng Anh")).Value);
        Assert.Equal(1, english.GetProperty("CurrentLevel").GetInt32());
        Assert.Equal(0, english.GetProperty("ConsecutiveFailures").GetInt32());
    }

    [Fact]
    public async Task ConcurrentRankUpdatesRejectLostStreakChanges()
    {
        await Seed();
        await Play(4);
        await using var first = Open();
        await using var second = Open();
        var a = await first.TowerRanks.SingleAsync();
        var b = await second.TowerRanks.SingleAsync();
        TowerRankPolicy.Apply(a, 4, 4, 0, false);
        TowerRankPolicy.Apply(b, 4, 4, 0, false);
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateSubmissionThatAlreadyLoadedAttemptCannotIncrementStreakTwice()
    {
        await Seed();
        var id = await Start(4);
        await using var first = Open();
        await using var second = Open();
        await second.QuizAttempts.Include(item => item.Exam).SingleAsync(item => item.Id == id);
        var controller = Controller(first);
        var session = (await new QuizStore(first).GetSessionAsync(id))!;
        foreach (var question in session.Questions)
            Assert.IsType<OkObjectResult>(await controller.CheckStudyAnswer(id, new() { QuestionId = question.Id, OptionId = null }));
        Assert.IsType<OkObjectResult>(await controller.SubmitAttempt(id, new()));
        Assert.IsType<ConflictObjectResult>(await Controller(second).SubmitAttempt(id, new()));
        await using var verify = Open();
        Assert.Equal(1, (await verify.TowerRanks.SingleAsync()).ConsecutiveFailures);
    }

    [Fact]
    public void PartialStarsDoNotPassAndOldEpochsCannotChangeRank()
    {
        var rank = new TowerRank { CurrentLevel = 1000 };
        TowerRankPolicy.Apply(rank, 1000, 1000, 0, false);
        TowerRankPolicy.Apply(rank, 999, 1000, 0, true);
        Assert.Equal(1, rank.ConsecutiveFailures);
        TowerRankPolicy.Apply(rank, 1000, 1000, 0, true);
        Assert.Equal(1000, rank.CurrentLevel);
        Assert.Equal(0, rank.ConsecutiveFailures);
        Assert.False(TowerRankPolicy.Apply(rank, 1000, 1000, 0, false).CountsForRank);
    }
    public void Dispose() { if (File.Exists(path)) File.Delete(path); }
}
