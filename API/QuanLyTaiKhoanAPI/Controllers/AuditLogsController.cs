using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.Interfaces;

namespace QuanLyTaiKhoanAPI.Controllers;

[ApiController]
[Authorize]
[Permission(AppPermissions.ViewSystemLogs)]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLogService.GetAsync(query);
        return Ok(result);
    }
}
