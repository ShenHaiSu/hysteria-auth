using HysteriaAuth.Master.Data;
using Microsoft.EntityFrameworkCore;

namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// 节点密钥认证中间件 — 从 X-Node-Secret 请求头验证节点密钥，仅应用于 /api/v1/auth/* 和 /api/v1/nodes/* 路由。
/// </summary>
public class NodeAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NodeAuthMiddleware> _logger;

    private static readonly string[] ProtectedPaths = { "/api/v1/auth", "/api/v1/nodes" };

    public NodeAuthMiddleware(RequestDelegate next, ILogger<NodeAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? "";
        if (!ProtectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var nodeSecret = context.Request.Headers["X-Node-Secret"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(nodeSecret))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "unauthorized",
                    message = "未提供节点密钥",
                    requestId = context.Items["RequestId"]?.ToString() ?? ""
                }
            });
            return;
        }

        // 查询数据库验证节点密钥（支持多版本并行）
        var node = await dbContext.Nodes.FirstOrDefaultAsync(n =>
            n.SecretKey == nodeSecret && n.IsActive);

        if (node == null)
        {
            _logger.LogWarning("节点密钥无效 | RequestId: {RequestId}", context.Items["RequestId"]);
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "node_secret_invalid",
                    message = "节点密钥无效",
                    requestId = context.Items["RequestId"]?.ToString() ?? ""
                }
            });
            return;
        }

        // 将 NodeId 注入 HttpContext.Items
        context.Items["NodeId"] = node.Id;

        await _next(context);
    }
}

/// <summary>
/// NodeAuthMiddleware 扩展方法
/// </summary>
public static class NodeAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseNodeAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<NodeAuthMiddleware>();
    }
}
