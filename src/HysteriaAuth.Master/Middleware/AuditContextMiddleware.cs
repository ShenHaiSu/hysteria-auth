namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// 审计上下文中间件 — 将客户端的真实 IP 注入 HttpContext.Items。
/// AdminId 已在 JwtMiddleware 中注入。
/// JwtMiddleware 在 AuditContextMiddleware 之前执行。
/// </summary>
public class AuditContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuditContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 获取客户端 IP（优先取 X-Forwarded-For，否则用 RemoteIpAddress）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = !string.IsNullOrWhiteSpace(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        context.Items["ClientIp"] = clientIp;

        await _next(context);
    }
}

/// <summary>
/// AuditContextMiddleware 扩展方法
/// </summary>
public static class AuditContextMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditContextMiddleware>();
    }
}
