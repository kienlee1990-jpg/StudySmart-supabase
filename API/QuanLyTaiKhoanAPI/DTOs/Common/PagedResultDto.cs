namespace QuanLyTaiKhoanAPI.DTOs.Common;

public class PagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}
