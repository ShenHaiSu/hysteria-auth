using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using HysteriaAuth.Master.Config;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Tests.Unit.Services;

public class AdminServiceTests
{
    private readonly Mock<IAdminRepository> _adminRepoMock;
    private readonly AdminSettings _adminSettings;
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _auditService;
    private readonly AdminService _sut;

    // We construct JwtService with real settings for the mock to see real behavior
    private static JwtService CreateRealJwtService()
    {
        var settings = Options.Create(new JwtSettings
        {
            Secret = "this-is-a-test-secret-key-at-least-32-chars-long!!",
            Issuer = "hysteria-auth-master",
            Audience = "hysteria-auth-admin",
            ExpirationMinutes = 1440,
            RefreshWindowMinutes = 5
        });
        return new JwtService(settings);
    }

    public AdminServiceTests()
    {
        _adminRepoMock = new Mock<IAdminRepository>();
        _adminSettings = new AdminSettings
        {
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15,
            AuditLogRetentionDays = 365
        };
        _auditLogRepoMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_auditLogRepoMock.Object, _loggerMock.Object);

        // Use real JwtService so GenerateToken/GetExpirationTime work correctly
        _sut = new AdminService(
            _adminRepoMock.Object,
            CreateRealJwtService(),
            Options.Create(_adminSettings),
            _auditService);
    }

    private static Admin CreateAdmin(long id = 1, string username = "admin", string role = "super_admin",
        string password = "adminPassword123", int failedAttempts = 0, DateTime? lockedUntil = null)
    {
        return new Admin
        {
            Id = id,
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password, 12),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = failedAttempts,
            LockedUntil = lockedUntil
        };
    }

    // ============================================================
    // 5.4.1 管理员登录成功 → JWT Token + admin 信息
    // ============================================================
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var password = "adminPassword123";
        var admin = CreateAdmin(password: password);
        var request = new LoginRequest { Username = "admin", Password = password };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var result = await _sut.LoginAsync(request, "127.0.0.1");

        result.Token.Should().NotBeNullOrEmpty();
        result.Admin.Id.Should().Be(1);
        result.Admin.Username.Should().Be("admin");
        result.Admin.Role.Should().Be("super_admin");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _adminRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Admin>(a => a.FailedLoginAttempts == 0 && a.LockedUntil == null)),
            Times.Once);
    }

    // ============================================================
    // 5.4.2 密码错误 → invalid_credentials
    // ============================================================
    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsForbiddenException()
    {
        var admin = CreateAdmin(password: "correctPassword");
        var request = new LoginRequest { Username = "admin", Password = "wrongPassword" };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var act = () => _sut.LoginAsync(request, "127.0.0.1");
        var ex = await act.Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be("invalid_credentials");
    }

    // ============================================================
    // 5.4.3 登录失败计数：FailedLoginAttempts++
    // ============================================================
    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailedAttempts()
    {
        var admin = CreateAdmin(password: "correctPassword", failedAttempts: 2);
        var request = new LoginRequest { Username = "admin", Password = "wrongPassword" };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var act = () => _sut.LoginAsync(request, "127.0.0.1");
        await act.Should().ThrowAsync<ForbiddenException>();

        _adminRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Admin>(a => a.FailedLoginAttempts == 3)),
            Times.Once);
    }

    // ============================================================
    // 5.4.4 账号锁定：连续 5 次错误 → account_locked
    // ============================================================
    [Fact]
    public async Task LoginAsync_FiveFailedAttempts_LocksAccount()
    {
        var admin = CreateAdmin(password: "correctPassword", failedAttempts: 4);
        var request = new LoginRequest { Username = "admin", Password = "wrongPassword" };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var act = () => _sut.LoginAsync(request, "127.0.0.1");
        await act.Should().ThrowAsync<ForbiddenException>();

        _adminRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Admin>(a =>
                a.FailedLoginAttempts == 5 &&
                a.LockedUntil.HasValue &&
                a.LockedUntil.Value > DateTime.UtcNow)),
            Times.Once);
    }

    // ============================================================
    // 5.4.5 锁定过期：15 分钟后可登录
    // ============================================================
    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsAccountLocked()
    {
        var password = "adminPassword123";
        var admin = CreateAdmin(password: password, lockedUntil: DateTime.UtcNow.AddMinutes(10));
        var request = new LoginRequest { Username = "admin", Password = password };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var act = () => _sut.LoginAsync(request, "127.0.0.1");
        var ex = await act.Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be("account_locked");
    }

    [Fact]
    public async Task LoginAsync_LockExpired_AllowsLogin()
    {
        var password = "adminPassword123";
        var admin = CreateAdmin(password: password, lockedUntil: DateTime.UtcNow.AddMinutes(-1));
        var request = new LoginRequest { Username = "admin", Password = password };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        var result = await _sut.LoginAsync(request, "127.0.0.1");
        result.Token.Should().NotBeNullOrEmpty();
    }

    // ============================================================
    // 5.4.6 成功登录重置：FailedLoginAttempts = 0
    // ============================================================
    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailedAttempts()
    {
        var password = "adminPassword123";
        var admin = CreateAdmin(password: password, failedAttempts: 4, lockedUntil: null);
        var request = new LoginRequest { Username = "admin", Password = password };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(admin);

        await _sut.LoginAsync(request, "127.0.0.1");

        _adminRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Admin>(a => a.FailedLoginAttempts == 0 && a.LockedUntil == null)),
            Times.Once);
    }

    // ============================================================
    // 5.4.9 创建管理员 → 成功
    // ============================================================
    [Fact]
    public async Task CreateAdminAsync_ValidRequest_ReturnsAdminDto()
    {
        var request = new CreateAdminRequest
        {
            Username = "new_admin", Password = "securePass123", Role = "admin"
        };

        _adminRepoMock.Setup(r => r.GetByUsernameAsync("new_admin")).ReturnsAsync((Admin?)null);
        _adminRepoMock.Setup(r => r.AddAsync(It.IsAny<Admin>()))
            .ReturnsAsync((Admin a) => { a.Id = 2; return a; });

        var result = await _sut.CreateAdminAsync(request, 1, "127.0.0.1");

        result.Id.Should().Be(2);
        result.Username.Should().Be("new_admin");
        result.Role.Should().Be("admin");
        result.IsActive.Should().BeTrue();
    }

    // ============================================================
    // 重复管理员名 → conflict
    // ============================================================
    [Fact]
    public async Task CreateAdminAsync_DuplicateUsername_ThrowsConflictException()
    {
        var request = new CreateAdminRequest { Username = "existing_admin", Password = "pass123" };
        _adminRepoMock.Setup(r => r.GetByUsernameAsync("existing_admin")).ReturnsAsync(CreateAdmin());

        var act = () => _sut.CreateAdminAsync(request, 1, "127.0.0.1");
        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.ErrorCode.Should().Be("conflict");
    }

    // ============================================================
    // 管理员不存在 → invalid_credentials
    // ============================================================
    [Fact]
    public async Task LoginAsync_AdminNotFound_ThrowsForbiddenException()
    {
        _adminRepoMock.Setup(r => r.GetByUsernameAsync("no_such_admin")).ReturnsAsync((Admin?)null);
        var request = new LoginRequest { Username = "no_such_admin", Password = "pass" };

        var act = () => _sut.LoginAsync(request, "127.0.0.1");
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
