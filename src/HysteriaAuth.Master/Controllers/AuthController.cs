using Microsoft.AspNetCore.Mvc;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Master.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Hysteria 认证端点（由 Edge Agent 调用，需要 X-Node-Secret 认证）
    /// </summary>
    [HttpPost("hysteria")]
    public async Task<IActionResult> AuthenticateHysteria([FromBody] AuthRequest request)
    {
        var result = await _authService.AuthenticateAsync(request);
        return Ok(result);
    }
}
