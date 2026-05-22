using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Tests.Unit.Services;

public class TrafficServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ITrafficRepository> _trafficRepoMock;
    private readonly Mock<ISessionRepository> _sessionRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly TrafficService _sut;

    public TrafficServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TrafficTest_{Guid.NewGuid()}").Options;
        _context = new AppDbContext(options);

        _trafficRepoMock = new Mock<ITrafficRepository>();
        _sessionRepoMock = new Mock<ISessionRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Traffic:ConcurrentUpdateRetryCount"] = "3"
            }).Build();

        _sut = new TrafficService(
            _trafficRepoMock.Object, _sessionRepoMock.Object,
            _userRepoMock.Object, _context, config,
            Mock.Of<ILogger<TrafficService>>());
    }

    public void Dispose() => _context.Dispose();

    private async Task<User> Seed(long id, string uname, long used = 0)
    {
        var u = new User { Id = id, Username = uname, Password = "h", TotalTrafficBytes = 10L * 1024 * 1024 * 1024, UsedTrafficBytes = used, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        return u;
    }

    [Fact] public async Task UpdateUsedTraffic_SingleThread_Correct()
    {
        var u = await Seed(1, "u1");
        await _sut.UpdateUsedTrafficAsync(u.Id, 1024);
        var upd = await _context.Users.FindAsync(1L);
        Assert.Equal(1024, upd!.UsedTrafficBytes);
    }

    [Fact] public async Task CheckIdempotency_NewKey_False()
    {
        _trafficRepoMock.Setup(r => r.CheckIdempotencyKeyExistsAsync("nk")).ReturnsAsync(false);
        Assert.False(await _trafficRepoMock.Object.CheckIdempotencyKeyExistsAsync("nk"));
    }

    [Fact] public async Task CheckIdempotency_DupKey_True()
    {
        _trafficRepoMock.Setup(r => r.CheckIdempotencyKeyExistsAsync("dk")).ReturnsAsync(true);
        Assert.True(await _trafficRepoMock.Object.CheckIdempotencyKeyExistsAsync("dk"));
    }

    [Fact] public void IdempotencyKey_25s_RoundsTo00()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 25, DateTimeKind.Utc);
        Assert.Equal("edge-node-01_user123_2025-01-01T12:00:00Z", TrafficService.GenerateIdempotencyKey("edge-node-01", "user123", t));
    }

    [Fact] public void IdempotencyKey_35s_RoundsTo30()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 35, DateTimeKind.Utc);
        Assert.Equal("edge-node-01_user123_2025-01-01T12:00:30Z", TrafficService.GenerateIdempotencyKey("edge-node-01", "user123", t));
    }

    [Fact] public void IdempotencyKey_59s_RoundsTo30()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 59, DateTimeKind.Utc);
        Assert.Equal("edge-node-01_user123_2025-01-01T12:00:30Z", TrafficService.GenerateIdempotencyKey("edge-node-01", "user123", t));
    }

    [Fact] public void IdempotencyKey_Exact30_Matches()
    {
        var t = new DateTime(2025, 6, 15, 8, 30, 0, DateTimeKind.Utc);
        Assert.Equal("node-02_admin_2025-06-15T08:30:00Z", TrafficService.GenerateIdempotencyKey("node-02", "admin", t));
    }

    [Fact] public async Task UpdateUsedTraffic_MultipleUpdates_Accumulates()
    {
        var u = await Seed(2, "a", 100);
        await _sut.UpdateUsedTrafficAsync(u.Id, 500);
        await _sut.UpdateUsedTrafficAsync(u.Id, 300);
        Assert.Equal(900, (await _context.Users.FindAsync(2L))!.UsedTrafficBytes);
    }

    [Fact] public async Task UpdateUsedTraffic_UserNotFound_NoThrow()
    {
        await _sut.UpdateUsedTrafficAsync(9999, 1024);
    }

    [Fact]
    public async Task UpdateSessions_NewOnlineUser_CreatesSession()
    {
        var u = await Seed(10, "new_online_user");
        _sessionRepoMock.Setup(r => r.GetActiveSessionsByNodeAsync("node-01")).ReturnsAsync(new List<Session>());
        _userRepoMock.Setup(r => r.GetByUsernameAsync("new_online_user")).ReturnsAsync(u);

        // UpdateSessionsAsync uses _context.Sessions.Add but does NOT call SaveChangesAsync internally
        await _sut.UpdateSessionsAsync("node-01", new Dictionary<string, int> { { "new_online_user", 1 } });
        await _context.SaveChangesAsync(); // explicit save

        var sessions = _context.Sessions.ToList();
        Assert.Contains(sessions, s => s.UserId == 10 && s.NodeId == "node-01" && s.Status == "active");
    }
}
