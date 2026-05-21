namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// 业务异常基类，包含错误代码和 HTTP 状态码，由 ExceptionMiddleware 统一捕获并格式化。
/// </summary>
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AppException(string errorCode, string message, int statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base("not_found", message, 404) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base("conflict", message, 409) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base("unauthorized", message, 401) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string errorCode, string message)
        : base(errorCode, message, 403) { }
}

public class ValidationException : AppException
{
    public ValidationException(string message)
        : base("validation_error", message, 422) { }
}
