using System.Security.Claims;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// JWT 认证中间件 — 处理 Authorization: Bearer {token}，仅应用于 /api/v1/admin/* 和 /api/v1/users/* 路由。
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    // 需要 JWT 认证的路由前缀
    private static readonly string[] ProtectedPaths = { "/api/v1/admin", "/api/v1/users", "/api/v1/nodes" };
    // 免认证路径（登录等）
    private static readonly string[] ExcludedPaths = { "/api/v1/admin/login" };

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, JwtService jwtService)
    {
        // 仅对管理相关路由启用 JWT 认证
        var path = context.Request.Path.Value ?? "";
        if (!ProtectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // 跳过免认证路径（如登录端点）
        if (ExcludedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "unauthorized",
                    message = "未提供认证凭据",
                    requestId = context.Items["RequestId"]?.ToString() ?? ""
                }
            });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var principal = jwtService.ValidateToken(token);

        if (principal == null)
        {
            _logger.LogWarning("JWT Token 无效或已过期 | RequestId: {RequestId}", context.Items["RequestId"]);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "token_expired",
                    message = "Token 已过期或无效",
                    requestId = context.Items["RequestId"]?.ToString() ?? ""
                }
            });
            return;
        }

        // 将 AdminId 和 Role 注入 HttpContext.Items
        var adminIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

        if (adminIdClaim != null)
            context.Items["AdminId"] = long.Parse(adminIdClaim);
        if (roleClaim != null)
            context.Items["AdminRole"] = roleClaim;

        await _next(context);
    }
}

/// <summary>
/// JwtMiddleware 扩展方法
/// </summary>
public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}
