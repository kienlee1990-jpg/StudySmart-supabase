namespace QuanLyTaiKhoanAPI.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new()
        {
            Success = true,
            Code = "SUCCESS",
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Fail(
        string message,
        string code = "ERROR",
        Dictionary<string, string[]>? errors = null)
        => new()
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default,
            Errors = errors
        };
}
