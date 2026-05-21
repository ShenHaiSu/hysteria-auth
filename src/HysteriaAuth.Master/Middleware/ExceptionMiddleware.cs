using System.Text;
using System.Text.Json;
using HysteriaAuth.Master.Models.DTOs;

namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// 全局异常处理中间件。捕获所有未处理异常，生成 RequestId，返回统一错误格式。
/// 必须在管道最前面注册。
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 生成 RequestId
        var requestId = $"req_{Guid.NewGuid():N}"[..15]; // req_ + 12 hex chars
        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // 业务异常 — 使用预定义的错误代码和状态码
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = ex.ErrorCode,
                    Message = ex.Message,
                    RequestId = requestId
                }
            };

            var json = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(json);

            _logger.LogWarning(ex, "业务异常: {ErrorCode} (HTTP {StatusCode}) | RequestId: {RequestId}",
                ex.ErrorCode, ex.StatusCode, requestId);
        }
        catch (Exception ex)
        {
            // 系统异常 — 统一返回 internal_error
            _logger.LogError(ex, "未处理系统异常 | RequestId: {RequestId}", requestId);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "internal_error",
                    Message = "服务器内部错误",
                    RequestId = requestId
                }
            };

            var json = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(json);
        }
    }
}

/// <summary>
/// ExceptionMiddleware 扩展方法
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}
