using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _auditService;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _auditLogRepoMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_auditLogRepoMock.Object, _loggerMock.Object);
        _sut = new UserService(_userRepoMock.Object, _auditService);
    }

    private static User CreateUser(long id = 1, string username = "testuser", long totalTraffic = 10L * 1024 * 1024 * 1024, long usedTraffic = 0)
    {
        return new User
        {
            Id = id,
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword("testPassword123", 12),
            Email = "test@example.com",
            TotalTrafficBytes = totalTraffic,
            UsedTrafficBytes = usedTraffic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };
    }

    // ============================================================
    // 5.3.1 创建用户：正常创建 → 返回 201 + 用户数据，密码 BCrypt 加密
    // ============================================================
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsUserDto()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "newuser",
            Password = "securePassword123",
            Email = "new@example.com",
            TotalTrafficBytes = 5L * 1024 * 1024 * 1024,
            IsActive = true
        };

        _userRepoMock.Setup(r => r.CheckUsernameExistsAsync("newuser")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = 1; return u; });

        // Act
        var result = await _sut.CreateAsync(request, 1, "127.0.0.1");

        // Assert
        result.Id.Should().Be(1);
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");
        result.TotalTrafficBytes.Should().Be(5L * 1024 * 1024 * 1024);
        result.IsActive.Should().BeTrue();
        result.UsedTrafficBytes.Should().Be(0);

        // Verify password was BCrypt hashed (not stored as plain text)
        _userRepoMock.Verify(r => r.AddAsync(
            It.Is<User>(u =>
                u.Password != "securePassword123" && // 密码已被加密
                u.Password.StartsWith("$2"))), // BCrypt hash 前缀
            Times.Once);
    }

    // ============================================================
    // 5.3.2 创建用户-用户名重复：→ conflict (409)
    // ============================================================
    [Fact]
    public async Task CreateAsync_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateUserRequest { Username = "existinguser", Password = "pass123" };
        _userRepoMock.Setup(r => r.CheckUsernameExistsAsync("existinguser")).ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateAsync(request, 1, "127.0.0.1");

        // Assert
        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.ErrorCode.Should().Be("conflict");
    }

    // ============================================================
    // 5.3.3 获取用户列表：分页 + 搜索 + 筛选
    // ============================================================
    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedList()
    {
        // Arrange
        var users = Enumerable.Range(1, 25).Select(i => CreateUser(i, $"user{i}")).ToList();
        _userRepoMock.Setup(r => r.GetAllAsync(1, 20, null, null, null))
            .ReturnsAsync((users.Take(20).ToList(), 25));

        // Act
        var result = await _sut.GetAllAsync(1, 20, null, null, null);

        // Assert
        result.Total.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(20);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersByUsername()
    {
        // Arrange
        var users = new List<User> { CreateUser(1, "special_user") };
        _userRepoMock.Setup(r => r.GetAllAsync(1, 20, "special", null, null))
            .ReturnsAsync((users, 1));

        // Act
        var result = await _sut.GetAllAsync(1, 20, "special", null, null);

        // Assert
        result.Total.Should().Be(1);
        result.Items[0].Username.Should().Be("special_user");
    }

    // ============================================================
    // 5.3.4 获取用户详情：存在的 ID → 200 + 完整数据
    // ============================================================
    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = CreateUser(5, "testuser");
        _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

        // Act
        var result = await _sut.GetByIdAsync(5);

        // Assert
        result.Id.Should().Be(5);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    // ============================================================
    // 5.3.5 获取用户详情-不存在：→ not_found (404)
    // ============================================================
    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.GetByIdAsync(999);

        // Assert
        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("not_found");
    }

    // ============================================================
    // 5.3.6 更新用户：部分更新 → 仅修改传递的字段
    // ============================================================
    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var user = CreateUser(1, "testuser", 10L * 1024 * 1024 * 1024);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            Email = "updated@example.com",
            Remark = "已更新备注"
        };

        // Act
        var result = await _sut.UpdateAsync(1, request, 1, "127.0.0.1");

        // Assert
        result.Email.Should().Be("updated@example.com");
        result.Remark.Should().Be("已更新备注");
        result.Username.Should().Be("testuser"); // 未修改
        result.TotalTrafficBytes.Should().Be(10L * 1024 * 1024 * 1024); // 未修改

        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.Email == "updated@example.com" &&
            u.Remark == "已更新备注")), Times.Once);
    }

    // ============================================================
    // 5.3.7 软删除用户：IsActive = false，历史数据保留
    // ============================================================
    [Fact]
    public async Task SoftDeleteAsync_ExistingUser_SetsIsActiveFalse()
    {
        // Arrange
        var user = CreateUser(1, "testuser");
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        await _sut.SoftDeleteAsync(1, 1, "127.0.0.1");

        // Assert
        _userRepoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.IsActive == false && u.Id == 1)),
            Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.SoftDeleteAsync(999, 1, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============================================================
    // 5.3.8 重置用户流量：UsedTrafficBytes = 0
    // ============================================================
    [Fact]
    public async Task ResetTrafficAsync_ExistingUser_SetsUsedTrafficToZero()
    {
        // Arrange
        var user = CreateUser(1, "testuser", usedTraffic: 5000);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        await _sut.ResetTrafficAsync(1, 1, "127.0.0.1");

        // Assert
        _userRepoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.UsedTrafficBytes == 0 && u.Id == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ResetTrafficAsync_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.ResetTrafficAsync(999, 1, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
