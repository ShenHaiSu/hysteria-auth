using Microsoft.AspNetCore.Mvc;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TrafficService _trafficService;

    public UsersController(UserService userService, TrafficService trafficService)
    {
        _userService = userService;
        _trafficService = trafficService;
    }

    /// <summary>
    /// 创建用户（管理员操作）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var user = await _userService.CreateAsync(request, adminId, clientIp);
        return CreatedAtAction(nameof(GetById), new { userId = user.Id }, user);
    }

    /// <summary>
    /// 获取用户列表（管理员操作）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? nodeId = null)
    {
        var result = await _userService.GetAllAsync(page, pageSize, search, isActive, nodeId);
        return Ok(result);
    }

    /// <summary>
    /// 获取用户详情（管理员操作）
    /// </summary>
    [HttpGet("{userId:long}")]
    public async Task<IActionResult> GetById(long userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// 更新用户（管理员操作，支持部分更新）
    /// </summary>
    [HttpPut("{userId:long}")]
    public async Task<IActionResult> Update(long userId, [FromBody] UpdateUserRequest request)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var user = await _userService.UpdateAsync(userId, request, adminId, clientIp);
        return Ok(user);
    }

    /// <summary>
    /// 软删除用户（管理员操作）
    /// </summary>
    [HttpDelete("{userId:long}")]
    public async Task<IActionResult> Delete(long userId)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        await _userService.SoftDeleteAsync(userId, adminId, clientIp);
        return NoContent();
    }

    /// <summary>
    /// 重置用户流量（管理员操作）
    /// </summary>
    [HttpPost("{userId:long}/reset-traffic")]
    public async Task<IActionResult> ResetTraffic(long userId)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        await _userService.ResetTrafficAsync(userId, adminId, clientIp);
        return Ok(new { message = "流量已重置" });
    }

    /// <summary>
    /// 获取用户流量统计（管理员操作，Phase 3）
    /// </summary>
    [HttpGet("{userId:long}/traffic-stats")]
    public async Task<IActionResult> GetTrafficStats(
        long userId,
        [FromQuery] string period = "month")
    {
        var stats = await _trafficService.GetUserTrafficStatsAsync(userId, period);
        return Ok(stats);
    }
}
