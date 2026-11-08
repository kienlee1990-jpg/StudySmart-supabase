using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs.Quiz;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Services;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizController : ControllerBase
{
    private const int TowerLevelCount = 1000;
    private const int TowerMaximumStars = 5;
    private readonly AppDbContext _db;
    private readonly QuizStore _store;

    public QuizController(AppDbContext db, QuizStore store)
    {
        _db = db;
        _store = store;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuizzes([FromQuery] int? grade, [FromQuery] string? subject, [FromQuery] string? mode)
    {
        var query = _db.QuizExams.AsNoTracking().Where(item => item.IsPublished && item.Questions.Any());
        if (grade.HasValue) query = query.Where(item => item.Grade == grade.Value);
        if (!string.IsNullOrWhiteSpace(subject)) query = query.Where(item => item.Subject == subject);
        if (!string.IsNullOrWhiteSpace(mode)) query = query.Where(item => item.Mode == mode);

        var quizzes = await query.OrderBy(item => item.Grade).ThenBy(item => item.Subject).ThenBy(item => item.Title)
            .Select(item => new
            {
                item.Id, item.Title, item.Description, item.Grade, item.Subject, item.Mode, item.DurationMinutes,
                QuestionCount = item.Questions.Count
            }).ToListAsync();
        return Ok(quizzes);
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessons([FromQuery] int grade, [FromQuery] string subject)
    {
        var lessons = await _db.QuizLessons.AsNoTracking()
            .Where(item => item.IsPublished && item.Grade == grade && item.Subject == subject)
            .OrderBy(item => item.Title)
            .Select(item => new { item.Id, item.Title, item.Summary, item.Content, item.Grade, item.Subject })
            .ToListAsync();
        return Ok(lessons);
    }

    [HttpGet("question-count")]
    public async Task<IActionResult> GetQuestionCount([FromQuery] int grade, [FromQuery] string subject)
    {
        var count = await _store.CountQuestionsAsync(grade, subject);
        return Ok(new { grade, subject, availableCount = count, requiredFor15Minutes = 10, requiredFor60Minutes = 40 });
    }

    [HttpGet("tower/progress")]
    public async Task<IActionResult> GetTowerProgress([FromQuery] string subject)
    {
        if (!IsAllowedSubject(subject)) return BadRequest(new { message = "Môn học không hợp lệ." });
        var (userId, guestId) = ResolveProgressOwner();
        if (userId == null && guestId == null)
            return BadRequest(new { message = "Thiếu mã phiên học sinh. Hãy tải lại trang rồi thử lại." });

        var progressQuery = _db.TowerProgress.Where(item => item.Subject == subject);
        var progress = userId != null
            ? await progressQuery.Where(item => item.UserId == userId).ToListAsync()
            : await progressQuery.Where(item => item.GuestId == guestId).ToListAsync();

        var attemptsQuery = _db.QuizAttempts.AsNoTracking().Where(item =>
            item.SubmittedAt != null && item.Score != null && item.Exam.Mode == "Tower" && item.Exam.Subject == subject);
        attemptsQuery = userId != null
            ? attemptsQuery.Where(item => item.UserId == userId)
            : attemptsQuery.Where(item => item.GuestId == guestId);
        var pastTowerAttempts = await attemptsQuery
            .Select(item => new { item.Exam.TowerLevel, item.Exam.Grade, item.Score, item.TotalQuestions })
            .ToListAsync();
        var bestStarsByLevel = pastTowerAttempts
            .Where(item => item.TotalQuestions > 0)
            .GroupBy(item => item.TowerLevel ?? Math.Clamp(item.Grade - 4, 1, TowerLevelCount))
            .ToDictionary(group => group.Key, group => group.Max(item => CalculateTowerStars(
                (int)Math.Round(item.Score!.Value * item.TotalQuestions / 100m, MidpointRounding.AwayFromZero),
                item.TotalQuestions)));
        var progressByLevel = progress.ToDictionary(item => item.Level);
        var progressChanged = false;
        foreach (var (level, bestStars) in bestStarsByLevel)
        {
            if (progressByLevel.TryGetValue(level, out var savedProgress))
            {
                if (savedProgress.Stars != bestStars)
                {
                    savedProgress.Stars = bestStars;
                    savedProgress.UpdatedAt = DateTime.UtcNow;
                    progressChanged = true;
                }
                continue;
            }

            var newProgress = new TowerProgress
            {
                UserId = userId,
                GuestId = guestId,
                Subject = subject,
                Level = level,
                Stars = bestStars,
                UpdatedAt = DateTime.UtcNow
            };
            _db.TowerProgress.Add(newProgress);
            progress.Add(newProgress);
            progressByLevel[level] = newProgress;
            progressChanged = true;
        }
        if (progressChanged) await _db.SaveChangesAsync();

        var starsByLevel = progress.ToDictionary(item => item.Level, item => item.Stars);
        var questionCounts = await _db.QuizQuestions.AsNoTracking()
            .Where(question => question.IsActive && question.Subject == subject)
            .GroupBy(question => question.Grade)
            .Select(group => new { Grade = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Grade, item => item.Count);
        var levels = Enumerable.Range(1, TowerLevelCount).Select(level =>
        {
            var stars = starsByLevel.GetValueOrDefault(level);
            var (questionGrade, requiredQuestions) = GetTowerLevelRules(level);
            return new
            {
                Level = level,
                QuestionGrade = questionGrade,
                Stars = stars,
                MaxStars = TowerMaximumStars,
                IsUnlocked = level == 1 || starsByLevel.GetValueOrDefault(level - 1) >= TowerMaximumStars,
                IsCompleted = stars >= TowerMaximumStars,
                QuestionCount = questionCounts.GetValueOrDefault(questionGrade),
                RequiredQuestions = requiredQuestions
            };
        }).ToList();
        return Ok(new { Subject = subject, StarRules = "90%+: 5 sao; 80–89%: 4 sao; 70–79%: 3 sao; 60–69%: 2 sao; 50–59%: 1 sao; dưới 50%: 0 sao.", Levels = levels });
    }

    [HttpPost("tower/sessions")]
    public async Task<IActionResult> StartTowerSession([FromBody] StartTowerLevelDto dto)
    {
        if (!IsAllowedSubject(dto.Subject)) return BadRequest(new { message = "Môn học không hợp lệ." });
        if (dto.Level is < 1 or > TowerLevelCount) return BadRequest(new { message = $"Tháp hiện có Level 1 đến Level {TowerLevelCount}." });
        var (userId, guestId) = ResolveProgressOwner();
        if (userId == null && guestId == null)
            return BadRequest(new { message = "Thiếu mã phiên học sinh. Hãy tải lại trang rồi thử lại." });

        if (dto.Level > 1)
        {
            var previousProgress = _db.TowerProgress.AsNoTracking().Where(item =>
                item.Subject == dto.Subject && item.Level == dto.Level - 1);
            var previousStars = userId != null
                ? await previousProgress.Where(item => item.UserId == userId).Select(item => (int?)item.Stars).FirstOrDefaultAsync()
                : await previousProgress.Where(item => item.GuestId == guestId).Select(item => (int?)item.Stars).FirstOrDefaultAsync();
            if (previousStars < TowerMaximumStars)
                return Conflict(new { message = $"Level {dto.Level} đang khóa. Hãy đạt đủ {TowerMaximumStars} sao ở Level {dto.Level - 1} trước nhé." });
        }

        var (questionGrade, requiredQuestions) = GetTowerLevelRules(dto.Level);
        var pool = await _store.GetQuestionsAsync(questionGrade, dto.Subject);
        if (pool.Count < requiredQuestions)
            return BadRequest(new { message = $"Level {dto.Level} cần {requiredQuestions} câu môn {dto.Subject} từ ngân hàng lớp {questionGrade}; hiện có {pool.Count}." });

        var chosen = pool.OrderBy(_ => Random.Shared.Next()).Take(requiredQuestions).ToList();
        var exam = new QuizExam
        {
            Title = $"Leo tháp · Level {dto.Level} · {dto.Subject}",
            Description = $"{requiredQuestions} câu hỏi lớp {questionGrade}. Số sao của lượt này tính theo điểm: từ 90% nhận 5 sao; cứ mỗi 10% thấp hơn giảm 1 sao.",
            Grade = questionGrade,
            TowerLevel = dto.Level,
            Subject = dto.Subject,
            Mode = "Tower",
            DurationMinutes = 0,
            IsPublished = false
        };
        exam.Questions = chosen.Select((question, index) => new ExamQuestion
        {
            QuestionId = question.Id, SortOrder = index
        }).ToList();
        var attempt = new QuizAttempt
        {
            Exam = exam,
            UserId = userId,
            GuestId = guestId,
            TotalQuestions = chosen.Count
        };
        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            attempt = new { attempt.Id, attempt.StartedAt },
            level = dto.Level,
            maxStars = TowerMaximumStars,
            quiz = new
            {
                exam.Id, exam.Title, exam.Description, exam.Grade, exam.Subject, exam.Mode,
                exam.DurationMinutes, TowerLevel = dto.Level,
                Questions = chosen.Select(question => new
                {
                    question.Id, question.Content,
                    Options = question.Options.OrderBy(_ => Random.Shared.Next()).Select(option => new { option.Id, option.Content })
                })
            }
        });
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> StartRandomSession([FromBody] StartRandomQuizDto dto)
    {
        var count = dto.DurationMinutes == 15 ? 10 : dto.DurationMinutes == 60 ? 40 : 0;
        if (count == 0)
            return BadRequest(new { message = "Chỉ hỗ trợ bài 15 phút (10 câu) hoặc 60 phút (40 câu)." });
        if (dto.Mode is not ("Test" or "ExamRoom"))
            return BadRequest(new { message = "Chọn chế độ kiểm tra hoặc phòng thi." });

        var pool = await _store.GetQuestionsAsync(dto.Grade, dto.Subject);
        if (pool.Count < count)
            return BadRequest(new { message = $"Ngân hàng hiện có {pool.Count} câu cho môn {dto.Subject}, lớp {dto.Grade}; cần ít nhất {count} câu khác nhau. Hãy bổ sung câu hỏi trong trang quản trị." });

        var chosen = pool.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        var title = $"{(dto.Mode == "Test" ? "Kiểm tra" : "Thi thử")} {dto.DurationMinutes} phút · {dto.Subject} · Lớp {dto.Grade}";
        var exam = new QuizExam
        {
            Title = title,
            Description = $"Đề được tạo tự động từ ngân hàng câu hỏi gồm {count} câu ngẫu nhiên.",
            Grade = dto.Grade,
            Subject = dto.Subject,
            Mode = dto.Mode,
            DurationMinutes = dto.DurationMinutes,
            IsPublished = false,
        };
        exam.Questions = chosen.Select((question, index) => new ExamQuestion
        {
            QuestionId = question.Id, SortOrder = index
        }).ToList();
        var attempt = new QuizAttempt
        {
            Exam = exam,
            UserId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
            GuestId = User.Identity?.IsAuthenticated == true ? null : GetGuestIdHeader(),
            GuestName = User.Identity?.IsAuthenticated == true ? null : CleanGuestName(dto.GuestName),
            TotalQuestions = count
        };
        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            attempt = new { attempt.Id, attempt.StartedAt },
            quiz = new
            {
                exam.Id, exam.Title, exam.Description, exam.Grade, exam.Subject, exam.Mode, exam.DurationMinutes,
                Questions = chosen.Select(question => new
                {
                    question.Id, question.Content,
                    Options = question.Options.OrderBy(_ => Random.Shared.Next()).Select(option => new { option.Id, option.Content })
                })
            }
        });
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string subject, [FromQuery] int? grade, [FromQuery] int? durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(subject)) return BadRequest(new { message = "Chọn môn học để xem ranking." });
        var query = _db.QuizAttempts.AsNoTracking()
            .Where(item => item.Exam.Subject == subject && item.SubmittedAt != null && item.Score != null && item.Exam.Mode != "Study");
        if (grade.HasValue) query = query.Where(item => item.Exam.Grade == grade.Value);
        if (durationMinutes.HasValue) query = query.Where(item => item.Exam.DurationMinutes == durationMinutes.Value);

        var records = await query.Select(item => new
        {
            item.Id, item.UserId, item.GuestName,
            item.Score, item.StartedAt, item.SubmittedAt,
            item.Exam.Title, item.Exam.Grade
        }).ToListAsync();

        var ranked = records
            .GroupBy(item => item.UserId ?? $"guest:{item.GuestName}:{item.Id}")
            .Select(group => group.OrderByDescending(item => item.Score)
                .ThenBy(item => item.SubmittedAt - item.StartedAt).First())
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.SubmittedAt - item.StartedAt)
            .ThenBy(item => item.SubmittedAt)
            .Take(50)
            .Select((item, index) => new
            {
                Rank = index + 1,
                Name = item.UserId != null
                    ? $"Học sinh {item.UserId[^4..].ToUpperInvariant()}"
                    : item.GuestName ?? "Học sinh khách",
                item.Grade, item.Title, item.Score, item.SubmittedAt,
                DurationSeconds = Math.Max(0, (int)(item.SubmittedAt!.Value - item.StartedAt).TotalSeconds)
            });

        return Ok(ranked);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetQuiz(int id)
    {
        var quiz = await _db.QuizExams.AsNoTracking()
            .Where(item => item.Id == id && item.IsPublished && item.Questions.Any())
            .Select(item => new
            {
                item.Id, item.Title, item.Description, item.Grade, item.Subject, item.Mode, item.DurationMinutes,
                Questions = item.Questions.OrderBy(link => link.SortOrder).Select(link => new
                {
                    link.Question.Id, link.Question.Content,
                    Options = link.Question.Options.OrderBy(option => option.Id)
                        .Select(option => new { option.Id, option.Content }).ToList()
                }).ToList()
            }).FirstOrDefaultAsync();
        return quiz == null ? NotFound() : Ok(quiz);
    }

    [HttpPost("{id:int}/attempts")]
    public async Task<IActionResult> StartAttempt(int id, [FromBody] StartQuizAttemptDto? dto)
    {
        var quiz = await _db.QuizExams.Include(item => item.Questions)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsPublished && item.Questions.Any());
        if (quiz == null) return NotFound();
        if (quiz.Questions.Count == 0) return BadRequest(new { message = "Bài này không còn câu hỏi." });

        var attempt = new QuizAttempt
        {
            ExamId = quiz.Id,
            UserId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
            GuestId = User.Identity?.IsAuthenticated == true ? null : GetGuestIdHeader(),
            GuestName = User.Identity?.IsAuthenticated == true ? null : CleanGuestName(dto?.GuestName),
            TotalQuestions = quiz.Questions.Count
        };
        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return Ok(new { attempt.Id, attempt.StartedAt, quiz.DurationMinutes });
    }

    [HttpPost("attempts/{attemptId:int}/answer")]
    public async Task<IActionResult> CheckStudyAnswer(int attemptId, SubmitQuizAnswerDto dto)
    {
        var attempt = await _db.QuizAttempts.Include(item => item.Exam)
            .FirstOrDefaultAsync(item => item.Id == attemptId);
        if (attempt == null) return NotFound();
        if (attempt.SubmittedAt.HasValue || attempt.Exam.Mode != "Study")
            return BadRequest(new { message = "Chế độ này không hỗ trợ xem đáp án từng câu." });

        var currentUserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        var currentGuestId = GetGuestIdHeader();
        if ((attempt.UserId != null && attempt.UserId != currentUserId) ||
            (attempt.UserId == null && currentUserId != null) ||
            (attempt.GuestId != null && attempt.GuestId != currentGuestId))
            return Forbid();

        var session = await _store.GetSessionAsync(attemptId);
        if (session == null)
            return StatusCode(StatusCodes.Status410Gone, new { message = "Không tìm thấy câu hỏi của lượt học này." });
        var question = session.Questions.FirstOrDefault(item => item.Id == dto.QuestionId);
        if (question == null) return BadRequest(new { message = "Câu hỏi không thuộc lượt học này." });
        var option = dto.OptionId == null ? null : question.Options.FirstOrDefault(item => item.Id == dto.OptionId.Value);
        if (option == null) return BadRequest(new { message = "Hãy chọn một đáp án hợp lệ." });

        var correctOption = question.Options.First(item => item.IsCorrect);
        return Ok(new
        {
            IsCorrect = option.IsCorrect,
            CorrectOptionId = correctOption.Id,
            CorrectOption = correctOption.Content,
            question.Explanation
        });
    }

    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> SubmitAttempt(int attemptId, [FromBody] SubmitQuizAttemptDto dto)
    {
        var attempt = await _db.QuizAttempts.Include(item => item.Exam)
            .FirstOrDefaultAsync(item => item.Id == attemptId);
        if (attempt == null) return NotFound();

        var currentUserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        var currentGuestId = GetGuestIdHeader();
        if ((attempt.UserId != null && attempt.UserId != currentUserId) ||
            (attempt.UserId == null && currentUserId != null) ||
            (attempt.GuestId != null && attempt.GuestId != currentGuestId))
            return Forbid();
        if (attempt.SubmittedAt.HasValue) return Conflict(new { message = "Lượt làm bài này đã được nộp." });
        var session = await _store.GetSessionAsync(attemptId);
        if (session == null)
            return StatusCode(StatusCodes.Status410Gone, new { message = "Lượt làm bài cũ không có câu hỏi đã lưu. Hãy bắt đầu lượt mới." });

        var elapsed = DateTime.UtcNow - attempt.StartedAt;
        if (attempt.Exam.DurationMinutes > 0 && elapsed > TimeSpan.FromMinutes(attempt.Exam.DurationMinutes) + TimeSpan.FromSeconds(10))
            return BadRequest(new { message = "Đã hết thời gian làm bài." });

        var questions = session.Questions;
        var suppliedAnswers = dto.Answers.GroupBy(answer => answer.QuestionId).ToDictionary(group => group.Key, group => group.Last());
        if (suppliedAnswers.Keys.Any(questionId => questions.All(question => question.Id != questionId)))
            return BadRequest(new { message = "Câu trả lời không thuộc bài làm này." });

        var answers = new List<(QuizQuestion Question, int? OptionId, bool IsCorrect, QuizOption? CorrectOption)>();
        foreach (var question in questions)
        {
            suppliedAnswers.TryGetValue(question.Id, out var supplied);
            var option = supplied?.OptionId == null
                ? null
                : question.Options.FirstOrDefault(item => item.Id == supplied.OptionId.Value);
            if (supplied?.OptionId != null && option == null)
                return BadRequest(new { message = "Lựa chọn không thuộc câu hỏi." });
            answers.Add((question, option?.Id, option?.IsCorrect == true, question.Options.FirstOrDefault(item => item.IsCorrect)));
        }

        _db.QuizAnswers.AddRange(answers.Select(answer => new QuizAnswer
        {
            AttemptId = attempt.Id, QuestionId = answer.Question.Id,
            OptionId = answer.OptionId, IsCorrect = answer.IsCorrect
        }));
        var correctCount = answers.Count(answer => answer.IsCorrect);
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score = questions.Count == 0 ? 0 : Math.Round(100m * correctCount / questions.Count, 2);
        object? towerResult = null;
        if (attempt.Exam.Mode == "Tower")
        {
            var level = attempt.Exam.TowerLevel ?? Math.Clamp(attempt.Exam.Grade - 4, 1, TowerLevelCount);
            var attemptStars = CalculateTowerStars(correctCount, questions.Count);
            var progressQuery = _db.TowerProgress.Where(item => item.Subject == attempt.Exam.Subject && item.Level == level);
            var towerProgress = attempt.UserId != null
                ? await progressQuery.FirstOrDefaultAsync(item => item.UserId == attempt.UserId)
                : await progressQuery.FirstOrDefaultAsync(item => item.GuestId == attempt.GuestId);
            if (towerProgress == null)
            {
                towerProgress = new TowerProgress
                {
                    UserId = attempt.UserId,
                    GuestId = attempt.GuestId,
                    Subject = attempt.Exam.Subject,
                    Level = level,
                    Stars = 0
                };
                _db.TowerProgress.Add(towerProgress);
            }

            var previousBestStars = towerProgress.Stars;
            towerProgress.Stars = Math.Max(towerProgress.Stars, attemptStars);
            var earnedStar = towerProgress.Stars > previousBestStars;
            towerProgress.UpdatedAt = DateTime.UtcNow;
            towerResult = new
            {
                Level = level,
                Stars = towerProgress.Stars,
                MaxStars = TowerMaximumStars,
                AttemptStars = attemptStars,
                EarnedStar = earnedStar,
                Passed = attemptStars > 0,
                IsCompleted = towerProgress.Stars >= TowerMaximumStars,
                NextLevelUnlocked = towerProgress.Stars >= TowerMaximumStars,
            };
        }
        await _db.SaveChangesAsync();

        return Ok(new
        {
            attempt.Id, attempt.Exam.Title, attempt.Exam.Subject, attempt.Exam.Grade, Mode = attempt.Exam.Mode,
            attempt.Score, CorrectCount = correctCount, TotalQuestions = questions.Count,
            Tower = towerResult,
            Answers = answers.Select(answer => new
            {
                answer.Question.Id, answer.Question.Content, answer.OptionId, answer.IsCorrect,
                CorrectOptionId = answer.CorrectOption?.Id, CorrectOption = answer.CorrectOption?.Content,
                Explanation = answer.Question.Explanation
            })
        });
    }

    [Authorize]
    [HttpGet("my-attempts")]
    public async Task<IActionResult> GetMyAttempts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(item => item.UserId == userId && item.SubmittedAt != null)
            .OrderByDescending(item => item.SubmittedAt)
            .Select(item => new
            {
                item.Id, item.StartedAt, item.SubmittedAt, item.Score, item.TotalQuestions,
                item.Exam.Title, item.Exam.Grade, item.Exam.Subject, item.Exam.Mode
            }).ToListAsync();
        return Ok(attempts);
    }

    [Authorize]
    [HttpGet("my-attempts/{attemptId:int}")]
    public Task<IActionResult> GetMyAttempt(int attemptId) => GetAttemptResult(attemptId);

    [HttpGet("attempts/{attemptId:int}/result")]
    public async Task<IActionResult> GetAttemptResult(int attemptId)
    {
        var (userId, guestId) = ResolveProgressOwner();
        if (userId == null && guestId == null) return NotFound();
        var attempt = await _db.QuizAttempts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == attemptId && item.SubmittedAt != null &&
                ((userId != null && item.UserId == userId) ||
                 (userId == null && guestId != null && item.UserId == null && item.GuestId == guestId)));
        if (attempt == null) return NotFound();

        var exam = await _db.QuizExams.AsNoTracking().FirstAsync(item => item.Id == attempt.ExamId);
        var answers = await _db.QuizAnswers.AsNoTracking()
            .Where(answer => answer.AttemptId == attemptId).OrderBy(answer => answer.Id)
            .Select(answer => new
            {
                Id = answer.QuestionId, answer.Question.Content, answer.OptionId, answer.IsCorrect,
                CorrectOptionId = answer.Question.Options.Where(option => option.IsCorrect).Select(option => (int?)option.Id).FirstOrDefault(),
                CorrectOption = answer.Question.Options.Where(option => option.IsCorrect).Select(option => option.Content).FirstOrDefault(),
                answer.Question.Explanation
            }).ToListAsync();
        object? tower = null;
        if (exam.Mode == "Tower")
        {
            var level = exam.TowerLevel ?? Math.Clamp(exam.Grade - 4, 1, TowerLevelCount);
            var attemptStars = CalculateTowerStars(
                (int)Math.Round((attempt.Score ?? 0) * attempt.TotalQuestions / 100m), attempt.TotalQuestions);
            var stars = await _db.TowerProgress.AsNoTracking()
                .Where(item => item.Subject == exam.Subject && item.Level == level &&
                    ((userId != null && item.UserId == userId) ||
                     (userId == null && item.GuestId == guestId)))
                .Select(item => (int?)item.Stars).FirstOrDefaultAsync() ?? attemptStars;
            tower = new { Level = level, Stars = stars, MaxStars = TowerMaximumStars,
                AttemptStars = attemptStars, EarnedStar = false, Passed = attemptStars > 0,
                IsCompleted = stars >= TowerMaximumStars, NextLevelUnlocked = stars >= TowerMaximumStars };
        }
        return Ok(new
        {
            attempt.Id, exam.Title, exam.Subject, exam.Grade, exam.Mode, Tower = tower,
            attempt.Score, CorrectCount = (int)Math.Round((attempt.Score ?? 0) * attempt.TotalQuestions / 100m),
            TotalQuestions = attempt.TotalQuestions,
            Answers = answers
        });
    }

    private static string? CleanGuestName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (int QuestionGrade, int RequiredQuestions) GetTowerLevelRules(int level)
    {
        var boundedLevel = Math.Clamp(level, 1, TowerLevelCount);
        var questionGrade = 5 + (int)(((long)(boundedLevel - 1) * 7) / (TowerLevelCount - 1));
        var requiredQuestions = 5 + (int)(((long)(boundedLevel - 1) * 15) / (TowerLevelCount - 1));
        return (questionGrade, requiredQuestions);
    }

    private static int CalculateTowerStars(int correctCount, int totalQuestions)
    {
        if (totalQuestions <= 0) return 0;
        var percentage = 100m * correctCount / totalQuestions;
        return percentage >= 90m ? 5
            : percentage >= 80m ? 4
            : percentage >= 70m ? 3
            : percentage >= 60m ? 2
            : percentage >= 50m ? 1
            : 0;
    }

    private (string? UserId, string? GuestId) ResolveProgressOwner()
    {
        if (User.Identity?.IsAuthenticated == true)
            return (User.FindFirstValue(ClaimTypes.NameIdentifier), null);
        return (null, GetGuestIdHeader());
    }

    private string? GetGuestIdHeader()
    {
        var value = Request.Headers["X-Guest-Id"].FirstOrDefault();
        return Guid.TryParse(value, out var guestId) ? guestId.ToString("N") : null;
    }

    private static bool IsAllowedSubject(string subject)
        => subject is "Toán" or "Tiếng Anh" or "Tiếng Việt";
}

public sealed class StartRandomQuizDto
{
    [Range(5, 12)]
    public int Grade { get; set; }

    [Required, RegularExpression("^(Toán|Tiếng Anh|Tiếng Việt)$")]
    public string Subject { get; set; } = string.Empty;

    [Required, RegularExpression("^(Test|ExamRoom)$")]
    public string Mode { get; set; } = "Test";

    [Range(15, 60)]
    public int DurationMinutes { get; set; }

    [MaxLength(100)]
    public string? GuestName { get; set; }
}

public sealed class StartTowerLevelDto
{
    [Required, RegularExpression("^(Toán|Tiếng Anh|Tiếng Việt)$")]
    public string Subject { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Level { get; set; }
}
