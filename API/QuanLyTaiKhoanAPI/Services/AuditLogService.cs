using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.DTOs.Common;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Interfaces;
using System.Security.Claims;

namespace QuanLyTaiKhoanAPI.Services;

public class AuditLogService : IAuditLogService
{
    private const int MaxPageSize = 100;
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Action))
        {
            return;
        }

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var actorUserId = entry.ActorUserId ?? httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var actorUserName = entry.ActorUserName
                ?? httpContext?.User.FindFirstValue(ClaimTypes.Email)
                ?? httpContext?.User.Identity?.Name;

            var auditLog = new AuditLog
            {
                Action = Truncate(entry.Action, 100)!,
                Succeeded = entry.Succeeded,
                ActorUserId = Truncate(actorUserId, 450),
                ActorUserName = Truncate(actorUserName, 256),
                TargetUserId = Truncate(entry.TargetUserId, 450),
                TargetUserName = Truncate(entry.TargetUserName, 256),
                EntityName = Truncate(entry.EntityName, 100),
                EntityId = Truncate(entry.EntityId, 450),
                Details = Truncate(entry.Details, 2000),
                IpAddress = Truncate(GetIpAddress(httpContext), 64),
                UserAgent = Truncate(httpContext?.Request.Headers["User-Agent"].ToString(), 512),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", entry.Action);
        }
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var auditLogs = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditLogs = auditLogs.Where(item => item.Action == query.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            auditLogs = auditLogs.Where(item => item.ActorUserId == query.ActorUserId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.TargetUserId))
        {
            auditLogs = auditLogs.Where(item => item.TargetUserId == query.TargetUserId.Trim());
        }

        if (query.FromUtc.HasValue)
        {
            var fromUtc = NormalizeUtc(query.FromUtc.Value);
            auditLogs = auditLogs.Where(item => item.CreatedAt >= fromUtc);
        }

        if (query.ToUtc.HasValue)
        {
            var toUtc = NormalizeUtc(query.ToUtc.Value);
            auditLogs = auditLogs.Where(item => item.CreatedAt <= toUtc);
        }

        var totalItems = await auditLogs.CountAsync(cancellationToken);
        var items = await auditLogs
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditLogDto
            {
                Id = item.Id,
                Action = item.Action,
                Succeeded = item.Succeeded,
                ActorUserId = item.ActorUserId,
                ActorUserName = item.ActorUserName,
                TargetUserId = item.TargetUserId,
                TargetUserName = item.TargetUserName,
                EntityName = item.EntityName,
                EntityId = item.EntityId,
                Details = item.Details,
                IpAddress = item.IpAddress,
                UserAgent = item.UserAgent,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AuditLogDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize),
            Items = items
        };
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    private static string? GetIpAddress(HttpContext? context)
    {
        var forwardedFor = context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context?.Connection.RemoteIpAddress?.ToString();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
