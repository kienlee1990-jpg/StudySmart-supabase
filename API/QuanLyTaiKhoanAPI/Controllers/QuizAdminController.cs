using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs.Quiz;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Middlewares;
using QuanLyTaiKhoanAPI.Services;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Authorize]
[Permission(AppPermissions.ManageQuizContent)]
[Route("api/quiz-admin")]
public class QuizAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly QuizStore _store;

    public QuizAdminController(AppDbContext db, QuizStore store)
    {
        _db = db;
        _store = store;
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessons()
    {
        var lessons = await _db.QuizLessons.AsNoTracking().OrderBy(item => item.Grade).ThenBy(item => item.Subject).ThenBy(item => item.Title)
            .Select(item => new { item.Id, item.Title, item.Topic, item.Summary, item.Content, item.VideoUrl, item.Grade, item.Subject, item.IsPublished })
            .ToListAsync();
        return Ok(lessons);
    }

    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson(CreateQuizLessonDto dto)
    {
        var lesson = new QuizLesson
        {
            Title = dto.Title.Trim(), Topic = Clean(dto.Topic), Summary = Clean(dto.Summary), Content = dto.Content.Trim(),
            VideoUrl = Clean(dto.VideoUrl), Grade = dto.Grade, Subject = dto.Subject, IsPublished = true
        };
        _db.QuizLessons.Add(lesson);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLessons), new { }, new { lesson.Id });
    }

    [HttpPut("lessons/{id:int}")]
    public async Task<IActionResult> UpdateLesson(int id, CreateQuizLessonDto dto)
    {
        var lesson = await _db.QuizLessons.FirstOrDefaultAsync(item => item.Id == id);
        if (lesson == null) return NotFound();
        lesson.Title = dto.Title.Trim();
        lesson.Topic = Clean(dto.Topic);
        lesson.Summary = Clean(dto.Summary);
        lesson.Content = dto.Content.Trim();
        lesson.VideoUrl = Clean(dto.VideoUrl);
        lesson.Grade = dto.Grade;
        lesson.Subject = dto.Subject;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("lessons/{id:int}")]
    public async Task<IActionResult> DeleteLesson(int id)
    {
        var lesson = await _db.QuizLessons.FindAsync(id);
        if (lesson == null) return NotFound();
        _db.QuizLessons.Remove(lesson);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("questions/import-template")]
    public IActionResult DownloadQuestionExcelTemplate()
        => File(QuizExcelImporter.CreateTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mau-cau-hoi.xlsx");

    [HttpPost("questions/import-excel")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> ImportQuestionsFromExcel([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Hãy chọn tệp Excel .xlsx để nhập." });
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chỉ hỗ trợ tệp Excel .xlsx." });
        if (file.Length > 5_000_000)
            return BadRequest(new { message = "Tệp Excel không được lớn hơn 5 MB." });

        List<ImportedQuestionRow> rows;
        try
        {
            using var stream = file.OpenReadStream();
            rows = QuizExcelImporter.Read(stream);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or FormatException or InvalidOperationException)
        {
            return BadRequest(new { message = "Tệp Excel không đúng định dạng hoặc bị lỗi." });
        }

        var (imported, skipped) = await _store.AddQuestionsAsync(rows);
        return Ok(new { importedCount = imported, skippedCount = skipped,
            message = $"Đã lưu {imported} câu hỏi vào Supabase." });
    }

    [HttpDelete("questions")]
    public async Task<IActionResult> DeleteAllQuestions()
    {
        var deleted = await _store.DeleteAllQuestionsAsync();
        return Ok(new
        {
            deletedCount = deleted.Questions,
            message = $"Đã xóa vĩnh viễn {deleted.Questions} câu hỏi, {deleted.Options} lựa chọn, {deleted.ExamLinks} liên kết đề thi và {deleted.Answers} câu trả lời lịch sử."
        });
    }

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions([FromQuery] int? grade, [FromQuery] string? subject)
    {
        var questions = (await _store.GetQuestionsAsync(grade, subject)).Select(item => new
        {
            item.Id, item.Grade, item.Subject, item.Difficulty, item.Topic, item.Content, item.Explanation, item.IsActive,
            Options = item.Options.OrderBy(option => option.Id)
                .Select(option => new { option.Id, option.Content, option.IsCorrect }).ToList()
        });
        return Ok(questions);
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion(CreateQuizQuestionDto dto)
    {
        if (dto.Options.Count(option => option.IsCorrect) != 1)
            return BadRequest(new { message = "Mỗi câu hỏi phải có chính xác một đáp án đúng." });

        var question = await _store.AddQuestionAsync(dto.Grade, dto.Subject, dto.Content.Trim(), Clean(dto.Explanation),
            dto.Options.Select(option => (option.Content.Trim(), option.IsCorrect)), dto.Difficulty, Clean(dto.Topic));
        if (question == null)
            return Conflict(new { message = "Câu hỏi này đã có trong ngân hàng." });
        return CreatedAtAction(nameof(GetQuestions), new { }, new { question.Id });
    }

    [HttpPatch("questions/{id:int}/difficulty")]
    public async Task<IActionResult> SetQuestionDifficulty(int id, [FromBody] SetQuestionDifficultyDto dto)
    {
        if (dto.Difficulty is not ("Thường" or "Nâng cao"))
            return BadRequest(new { message = "Loại câu hỏi phải là Thường hoặc Nâng cao." });
        return await _store.SetQuestionDifficultyAsync(id, dto.Difficulty) ? NoContent() : NotFound();
    }

    [HttpPatch("questions/{id:int}/topic")]
    public async Task<IActionResult> SetQuestionTopic(int id, [FromBody] SetQuestionTopicDto dto)
    {
        return await _store.SetQuestionTopicAsync(id, Clean(dto.Topic)) ? NoContent() : NotFound();
    }

    [HttpDelete("questions/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        return await _store.RemoveQuestionAsync(id) ? NoContent() : NotFound();
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams()
    {
        var exams = await _db.QuizExams.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id, item.Title, item.Description, item.Grade, item.Subject, item.Mode,
                item.DurationMinutes, item.IsPublished, QuestionCount = item.Questions.Count
            }).ToListAsync();
        return Ok(exams);
    }

    [HttpPost("exams")]
    public IActionResult CreateExam(CreateQuizExamDto dto)
    {
        return BadRequest(new { message = "Không cần tạo đề trước. Hệ thống lấy câu hỏi trong ngân hàng khi học sinh bắt đầu bài kiểm tra hoặc phòng thi." });
    }

    [HttpPut("exams/{id:int}/publish")]
    public async Task<IActionResult> SetPublished(int id, [FromBody] SetPublishedDto dto)
    {
        var exam = await _db.QuizExams.FirstOrDefaultAsync(item => item.Id == id);
        if (exam == null) return NotFound();
        exam.IsPublished = dto.IsPublished;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class SetPublishedDto
    {
        public bool IsPublished { get; set; }
    }

public sealed class SetQuestionDifficultyDto
{
    [Required, RegularExpression("^(Thường|Nâng cao)$")]
    public string Difficulty { get; set; } = "Thường";
}

public sealed class SetQuestionTopicDto
{
    [MaxLength(200)]
    public string? Topic { get; set; }
}
}

public sealed class CreateQuizLessonDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Topic { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    [Required, MaxLength(20000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000), Url]
    public string? VideoUrl { get; set; }

    [Range(4, 12)]
    public int Grade { get; set; }

    [Required, RegularExpression("^(Toán|Tiếng Anh|Tiếng Việt)$")]
    public string Subject { get; set; } = string.Empty;
}
