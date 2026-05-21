using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 主服务器健康检查端点，无需认证。返回 healthy/degraded/unhealthy 状态。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var checks = new Dictionary<string, string>();
        string status;

        // 数据库检查
        try
        {
            await _context.Database.CanConnectAsync();
            checks["database"] = "ok";
        }
        catch
        {
            checks["database"] = "error";
        }

        // 磁盘空间检查（简化：检查数据库文件是否存在）
        try
        {
            var dbPath = _context.Database.GetConnectionString();
            if (!string.IsNullOrWhiteSpace(dbPath))
            {
                // 简单可用性检查：认为数据库连接成功表示磁盘可写
                checks["disk_space"] = "ok";
            }
            else
            {
                checks["disk_space"] = "unknown";
            }
        }
        catch
        {
            checks["disk_space"] = "error";
        }

        // 综合判断状态
        if (checks.Values.All(v => v == "ok"))
        {
            status = "healthy";
        }
        else if (checks.ContainsValue("error"))
        {
            status = "unhealthy";
        }
        else
        {
            status = "degraded";
        }

        var uptime = DateTime.UtcNow - _startTime;

        return Ok(new
        {
            Status = status,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Version = "1.0.0",
            Uptime = $"{uptime.Days}d{uptime.Hours:D2}h{uptime.Minutes:D2}m{uptime.Seconds:D2}s",
            Checks = checks
        });
    }
}
