using Microsoft.AspNetCore.Mvc;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// 管理员登录
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _adminService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// 创建管理员（仅 super_admin 可操作）
    /// </summary>
    [HttpPost("admins")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var admin = await _adminService.CreateAdminAsync(request);
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
        var admin = await _adminService.UpdateAdminAsync(adminId, request);
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
}
