using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("ai-chat")]
[Route("api/ai")]
public sealed class AiChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private const int MaxHistoryMessages = 8;
    private const int MaxTotalCharacters = 6000;
    private static readonly HashSet<string> AllowedSubjects = ["Toán", "Tiếng Anh", "Tiếng Việt"];

    [HttpPost("chat")]
    [RequestSizeLimit(20_000)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (!AllowedSubjects.Contains(request.Subject))
            return BadRequest(new { message = "Môn học không hợp lệ." });
        if (request.Grade is < 5 or > 12)
            return BadRequest(new { message = "Khối lớp chỉ nhận từ lớp 5 đến lớp 12." });
        if (request.Messages.Count is < 1 or > MaxHistoryMessages)
            return BadRequest(new { message = $"Mỗi lượt gửi cần từ 1 đến {MaxHistoryMessages} tin nhắn gần nhất." });
        if (!string.Equals(request.Messages[^1].Role, "user", StringComparison.Ordinal))
            return BadRequest(new { message = "Tin nhắn cuối cùng phải là câu hỏi của học sinh." });
        if (request.Messages.Any(message => message.Role is not ("user" or "assistant") || string.IsNullOrWhiteSpace(message.Content)))
            return BadRequest(new { message = "Lịch sử trò chuyện không hợp lệ." });
        if (request.Messages.Any(message => message.Content.Length > 2000) ||
            request.Messages.Sum(message => message.Content.Length) > MaxTotalCharacters)
            return BadRequest(new { message = "Mỗi tin tối đa 2.000 ký tự và tổng lịch sử tối đa 6.000 ký tự." });

        var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? configuration["Gemini:ApiKey"];
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? configuration["OpenAI:ApiKey"];
        var provider = configuration["AI:Provider"] ?? Environment.GetEnvironmentVariable("AI_PROVIDER");
        var useGemini = string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase) ||
            (string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(geminiKey));
        var apiKey = useGemini ? geminiKey : openAiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Chatbot chưa được cấu hình. Đặt GEMINI_API_KEY (Gemini) hoặc OPENAI_API_KEY (OpenAI), rồi khởi động lại API." });

        var instructions = $"""
            Bạn là Gia sư Học vui, trợ lý học tập thân thiện cho học sinh Việt Nam lớp {request.Grade}, môn {request.Subject}.
            Điều chỉnh kiến thức, thuật ngữ và độ sâu lời giải theo lớp {request.Grade}; chia lời giải thành các bước rõ ràng và dùng ví dụ phù hợp với lứa tuổi.
            Khi học sinh hỏi bài, trước tiên gợi mở bằng một câu hỏi hoặc gợi ý nhỏ; nếu các em cần, giải thích lời giải đầy đủ.
            Không bịa nội dung chương trình, đáp án hoặc nguồn tham khảo. Nếu đề bài thiếu dữ kiện, hỏi lại rõ ràng.
            Không yêu cầu hoặc nhắc học sinh gửi họ tên đầy đủ, địa chỉ, số điện thoại, mật khẩu hay thông tin riêng tư.
            Nếu được hỏi ngoài học tập hoặc nội dung không phù hợp với trẻ em, từ chối ngắn gọn và hướng các em quay lại việc học.
            Giữ câu trả lời ngắn gọn, tích cực, không dùng thuật ngữ khó nếu chưa giải thích.
            """;

        try
        {
            var client = httpClientFactory.CreateClient("AI");
            using var message = useGemini
                ? CreateGeminiRequest(apiKey, configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite", instructions, request.Messages)
                : CreateOpenAiRequest(apiKey, configuration["OpenAI:Model"] ?? "gpt-5-mini", instructions, request.Messages);
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = useGemini ? "Khóa Gemini không hợp lệ. Hãy kiểm tra GEMINI_API_KEY." : "Khóa OpenAI không hợp lệ. Hãy kiểm tra OPENAI_API_KEY." });
            if ((int)response.StatusCode == 429)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "AI đang bận hoặc vượt hạn mức. Em thử lại sau nhé." });
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "AI chưa trả lời được. Em thử lại sau nhé." });

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var answer = useGemini ? ReadGeminiAnswer(document.RootElement) : ReadOpenAiAnswer(document.RootElement);

            if (string.IsNullOrWhiteSpace(answer))
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "AI trả về nội dung rỗng. Em thử hỏi lại nhé." });
            return Ok(new { answer });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = "AI phản hồi quá lâu. Em thử gửi lại nhé." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Không kết nối được với dịch vụ AI." });
        }
        catch (JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "AI trả về dữ liệu không đọc được." });
        }
    }

    private static HttpRequestMessage CreateOpenAiRequest(string apiKey, string model, string instructions, IEnumerable<AiChatMessage> messages)
    {
        var payload = new
        {
            model,
            instructions,
            input = messages.Select(message => new { role = message.Role, content = message.Content.Trim() }),
            max_output_tokens = 600,
            store = false
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private static HttpRequestMessage CreateGeminiRequest(string apiKey, string model, string instructions, IEnumerable<AiChatMessage> messages)
    {
        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = instructions } } },
            contents = messages.Select(message => new
            {
                role = message.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = message.Content.Trim() } }
            }),
            generationConfig = new { maxOutputTokens = 600 }
        };
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private static string? ReadOpenAiAnswer(JsonElement root) => root.GetProperty("output").EnumerateArray()
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
                .SelectMany(item => item.GetProperty("content").EnumerateArray())
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() is "output_text" or "refusal")
                .Select(item => item.TryGetProperty("text", out var text) ? text.GetString() :
                    item.TryGetProperty("refusal", out var refusal) ? refusal.GetString() : null)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

    private static string? ReadGeminiAnswer(JsonElement root) => root.GetProperty("candidates").EnumerateArray()
        .SelectMany(candidate => candidate.GetProperty("content").GetProperty("parts").EnumerateArray())
        .Where(part => part.TryGetProperty("text", out _))
        .Select(part => part.GetProperty("text").GetString())
        .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
}

public sealed class AiChatRequest
{
    [Range(5, 12)]
    public int Grade { get; set; } = 5;

    [Required, MaxLength(30)]
    public string Subject { get; set; } = "Toán";

    [Required, MinLength(1), MaxLength(8)]
    public List<AiChatMessage> Messages { get; set; } = [];
}

public sealed class AiChatMessage
{
    [Required, RegularExpression("^(user|assistant)$")]
    public string Role { get; set; } = "user";

    [Required, MinLength(1), MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
