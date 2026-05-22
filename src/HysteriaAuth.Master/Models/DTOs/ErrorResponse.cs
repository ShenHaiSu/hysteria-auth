namespace HysteriaAuth.Master.Models.DTOs;

/// <summary>
/// 统一错误响应格式（所有 API 错误响应的唯一格式）。
/// </summary>
public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}

public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}
