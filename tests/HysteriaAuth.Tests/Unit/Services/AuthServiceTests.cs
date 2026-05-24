using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAuthLogRepository> _authLogRepoMock = new();
    private readonly Mock<INodeRepository> _nodeRepoMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuthTest_{Guid.NewGuid()}").Options;
        var dbCtx = new AppDbContext(dbOptions);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MasterServerUrl"] = "https://master.example.com",
                ["Traffic:ConcurrentUpdateRetryCount"] = "3"
            }).Build();

        var aes = new AesEncryptionService(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        var auditLogRepoMock = new Mock<IAuditLogRepository>();
        var auditLoggerMock = new Mock<ILogger<AuditService>>();
        var auditService = new AuditService(auditLogRepoMock.Object, auditLoggerMock.Object);

        var configGenerator = new ConfigGeneratorService(aes, config);

        var nodeService = new NodeService(
            _nodeRepoMock.Object, Mock.Of<INodeStatusRepository>(),
            Mock.Of<ITrafficRepository>(), dbCtx, aes,
            null!, null!, configGenerator, config,
            Mock.Of<ILogger<NodeService>>(), auditService);

        _sut = new AuthService(_userRepoMock.Object, _authLogRepoMock.Object, nodeService);
    }

    private static User U(string? pwd = null)
    {
        return new User { Id = 1, Username = "testuser", Password = pwd ?? BCrypt.Net.BCrypt.HashPassword("testPassword123", 12), IsActive = true, TotalTrafficBytes = 10L * 1024 * 1024 * 1024, UsedTrafficBytes = 0, ExpiresAt = null, AllowedNodes = null };
    }

    private static Node N(string id = "edge-node-01") => new() { Id = id, Name = "N", IpAddress = "10.0.0.1", Port = 443, IsActive = true, CreatedAt = DateTime.UtcNow };

    private AuthRequest R(string nodeId = "edge-node-01") => new() { Username = "testuser", Password = "testPassword123", NodeId = nodeId, ClientIp = "1.2.3.4" };

    [Fact] public async Task ValidCredentials_ReturnsSuccess()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U());
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        var r = await _sut.AuthenticateAsync(R());
        Assert.True(r.Success);
        _authLogRepoMock.Verify(x => x.AddAsync(It.Is<AuthLog>(l => l.Success)), Times.Once);
    }

    [Fact] public async Task WrongPassword_Throws()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U(BCrypt.Net.BCrypt.HashPassword("correct", 12)));
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(new AuthRequest { Username = "testuser", Password = "wrong", NodeId = "n", ClientIp = "1.2.3.4" }));
        Assert.Equal("invalid_credentials", ex.ErrorCode);
    }

    [Fact] public async Task UserNotFound_Throws()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("no")).ReturnsAsync((User?)null);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(new AuthRequest { Username = "no", Password = "x", NodeId = "n", ClientIp = "1.2.3.4" }));
        Assert.Equal("invalid_credentials", ex.ErrorCode);
    }

    [Fact] public async Task AccountDisabled_Throws()
    {
        var u = U(); u.IsActive = false;
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(R()));
        Assert.Equal("account_disabled", ex.ErrorCode);
    }

    [Fact] public async Task AccountExpired_Throws()
    {
        var u = U(); u.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(R()));
        Assert.Equal("account_expired", ex.ErrorCode);
    }

    [Fact] public async Task NullExpiresAt_Succeeds()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U());
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        Assert.True((await _sut.AuthenticateAsync(R())).Success);
    }

    [Fact] public async Task TrafficExhausted_Throws()
    {
        var u = U(); u.TotalTrafficBytes = 1024; u.UsedTrafficBytes = 1024;
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(R()));
        Assert.Equal("traffic_exhausted", ex.ErrorCode);
    }

    [Fact] public async Task SufficientTraffic_Succeeds()
    {
        var u = U(); u.TotalTrafficBytes = 1024; u.UsedTrafficBytes = 0;
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        Assert.True((await _sut.AuthenticateAsync(R())).Success);
    }

    [Fact] public async Task NodeNotInWhitelist_Throws()
    {
        var u = U(); u.AllowedNodes = "[\"node-other\"]";
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(R()));
        Assert.Equal("node_not_allowed", ex.ErrorCode);
    }

    [Fact] public async Task NodeInWhitelist_Succeeds()
    {
        var u = U(); u.AllowedNodes = "[\"edge-node-01\"]";
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(u);
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        Assert.True((await _sut.AuthenticateAsync(R())).Success);
    }

    [Fact] public async Task NullAllowedNodes_Succeeds()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U());
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        Assert.True((await _sut.AuthenticateAsync(R())).Success);
    }

    [Fact] public async Task NodeOffline_Throws()
    {
        var off = N(); off.IsActive = false;
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U());
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(off);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(R()));
        Assert.Equal("node_not_allowed", ex.ErrorCode);
    }

    [Fact] public async Task Success_WritesAuthLog()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(U());
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01")).ReturnsAsync(N());
        await _sut.AuthenticateAsync(new AuthRequest { Username = "testuser", Password = "testPassword123", NodeId = "edge-node-01", ClientIp = "10.0.0.50" });
        _authLogRepoMock.Verify(r => r.AddAsync(It.Is<AuthLog>(l => l.Success && l.ClientIp == "10.0.0.50")), Times.Once);
    }

    [Fact] public async Task CheckOrder_SkipsNodeWhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("nonexistent")).ReturnsAsync((User?)null);
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.AuthenticateAsync(new AuthRequest { Username = "nonexistent", Password = "x", NodeId = "n", ClientIp = "1.2.3.4" }));
        _nodeRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }
}
