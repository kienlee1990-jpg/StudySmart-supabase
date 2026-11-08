using System.Net;
using System.Text.Json;
using QuanLyTaiKhoanAPI.Responses;

namespace QuanLyTaiKhoanAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex, _environment);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, IWebHostEnvironment environment)
    {
        var statusCode = ex switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            BadRequestException => HttpStatusCode.BadRequest,
            ForbiddenException => HttpStatusCode.Forbidden,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        var message = statusCode == HttpStatusCode.InternalServerError && !environment.IsDevelopment()
            ? "Internal server error. Please contact administrator."
            : ex.Message;

        var code = statusCode switch
        {
            HttpStatusCode.NotFound => "NOT_FOUND",
            HttpStatusCode.BadRequest => "BAD_REQUEST",
            HttpStatusCode.Forbidden => "FORBIDDEN",
            HttpStatusCode.Unauthorized => "UNAUTHORIZED",
            _ => "INTERNAL_SERVER_ERROR"
        };

        var response = ApiResponse<string>.Fail(message, code);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
