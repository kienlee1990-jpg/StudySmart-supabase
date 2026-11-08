using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.DTOs.Common;

namespace QuanLyTaiKhoanAPI.Interfaces;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<PagedResultDto<AuditLogDto>> GetAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default);
}
