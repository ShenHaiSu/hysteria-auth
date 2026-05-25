using Microsoft.AspNetCore.Mvc;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Services;
using HysteriaAuth.Master.Middleware;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly KickService _kickService;
    private readonly AuditService _auditService;

    public AdminController(AdminService adminService, KickService kickService, AuditService auditService)
    {
        _adminService = adminService;
        _kickService = kickService;
        _auditService = auditService;
    }

    /// <summary>
    /// 管理员登录
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var response = await _adminService.LoginAsync(request, clientIp);
        return Ok(response);
    }

    /// <summary>
    /// 创建管理员（仅 super_admin 可操作）
    /// </summary>
    [HttpPost("admins")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var admin = await _adminService.CreateAdminAsync(request, adminId, clientIp);
        return CreatedAtAction(nameof(GetAdmin), new { adminId = admin.Id }, admin);
    }

    /// <summary>
    /// 获取管理员列表（仅 super_admin 可操作）
    /// </summary>
    [HttpGet("admins")]
    public async Task<IActionResult> GetAllAdmins(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var admins = await _adminService.GetAllAdminsAsync(page, pageSize);
        return Ok(admins);
    }

    /// <summary>
    /// 获取单个管理员
    /// </summary>
    [HttpGet("admins/{adminId:long}")]
    public IActionResult GetAdmin(long adminId)
    {
        // 由 AdminService 提供实现
        return Ok(new { id = adminId });
    }

    /// <summary>
    /// 更新管理员（仅 super_admin 可操作）
    /// </summary>
    [HttpPut("admins/{adminId:long}")]
    public async Task<IActionResult> UpdateAdmin(long adminId, [FromBody] UpdateAdminRequest request)
    {
        var currentAdminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var admin = await _adminService.UpdateAdminAsync(adminId, request, currentAdminId, clientIp);
        return Ok(admin);
    }

    /// <summary>
    /// 系统概览（管理员操作）
    /// </summary>
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        // Phase 1 返回占位数据，后续阶段逐步实现
        return Ok(new DashboardResponse
        {
            TotalUsers = 0,
            ActiveUsers = 0,
            TotalNodes = 0,
            ActiveNodes = 0,
            OnlineUsersNow = 0,
            TotalTrafficToday = 0,
            TotalTrafficThisMonth = 0
        });
    }

    /// <summary>
    /// 管理员踢用户下线（Phase 3）。
    /// 建议同时通过 PUT /api/v1/users/{userId} 将用户 isActive 设为 false。
    /// </summary>
    [HttpPost("kick-user")]
    public async Task<IActionResult> KickUser([FromBody] KickUserRequest request)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        await _kickService.AdminKickUserAsync(request, adminId, clientIp);
        return Ok(new { message = $"用户 {request.Username} 已踢下线" });
    }

    /// <summary>
    /// 获取审计日志（super_admin 和 admin 可调用）。
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] long? adminId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        // 权限检查：只读管理员不可查看审计日志
        var role = HttpContext.Items["AdminRole"]?.ToString();
        if (role == "readonly")
            throw new ForbiddenException("forbidden", "权限不足，无法查看审计日志");

        var query = new AuditLogQuery
        {
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 200),
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            StartTime = startTime,
            EndTime = endTime
        };

        var result = await _auditService.GetPagedAsync(query);
        return Ok(result);
    }
}
