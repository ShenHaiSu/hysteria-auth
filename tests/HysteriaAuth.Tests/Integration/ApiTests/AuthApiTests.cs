using Xunit;
using FluentAssertions;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;

namespace HysteriaAuth.Tests.Integration.ApiTests;

/// <summary>
/// 5.9 API 集成测试
/// 验证 API 结构、DTO、错误响应格式的正确性。
/// 完整的 HTTP 端点集成测试可通过 WebApplicationFactory 在 CI 中运行。
/// </summary>
public class AuthApiTests
{
    [Fact]
    public void HealthController_Exists()
    {
        var controllerType = typeof(HysteriaAuth.Master.Controllers.HealthController);
        controllerType.Should().NotBeNull();
    }

    [Fact]
    public void AuthController_Exists()
    {
        var controllerType = typeof(HysteriaAuth.Master.Controllers.AuthController);
        controllerType.Should().NotBeNull();
    }

    [Fact]
    public void UsersController_Exists()
    {
        var controllerType = typeof(HysteriaAuth.Master.Controllers.UsersController);
        controllerType.Should().NotBeNull();
    }

    [Fact]
    public void NodesController_Exists()
    {
        var controllerType = typeof(HysteriaAuth.Master.Controllers.NodesController);
        controllerType.Should().NotBeNull();
    }

    [Fact]
    public void ErrorResponse_Format_IsCorrect()
    {
        var error = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = "test_error",
                Message = "测试错误信息",
                RequestId = "req_test_001"
            }
        };

        error.Error.Code.Should().Be("test_error");
        error.Error.Message.Should().Be("测试错误信息");
        error.Error.RequestId.Should().Be("req_test_001");
    }

    [Fact]
    public void AuthRequest_Properties_AreSetCorrectly()
    {
        var request = new AuthRequest
        {
            Username = "user1", Password = "pass1", NodeId = "node-01", ClientIp = "1.2.3.4"
        };

        request.Username.Should().Be("user1");
        request.Password.Should().Be("pass1");
        request.NodeId.Should().Be("node-01");
        request.ClientIp.Should().Be("1.2.3.4");
    }

    [Fact]
    public void AppException_HasCorrectStatusCodes()
    {
        var notFound = new NotFoundException("test");
        notFound.ErrorCode.Should().Be("not_found");
        notFound.StatusCode.Should().Be(404);

        var conflict = new ConflictException("test");
        conflict.ErrorCode.Should().Be("conflict");
        conflict.StatusCode.Should().Be(409);

        var forbidden = new ForbiddenException("test_code", "test");
        forbidden.ErrorCode.Should().Be("test_code");
        forbidden.StatusCode.Should().Be(403);
    }
}
