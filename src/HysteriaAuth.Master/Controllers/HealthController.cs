using Microsoft.AspNetCore.Mvc;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 健康检查端点，无需认证
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Version = "1.0.0"
        });
    }
}
