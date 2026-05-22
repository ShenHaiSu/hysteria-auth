using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Tests.Integration.DatabaseTests;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DbTest_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ============================================================
    // 5.10.1 EF Core Model Creation
    // ============================================================
    [Fact]
    public async Task DatabaseCreation_AllDbSetsAccessible()
    {
        await _context.Database.EnsureCreatedAsync();

        // Verify all DbSets are accessible
        _context.Users.Should().NotBeNull();
        _context.Nodes.Should().NotBeNull();
        _context.TrafficRecords.Should().NotBeNull();
        _context.AuthLogs.Should().NotBeNull();
        _context.Sessions.Should().NotBeNull();
        _context.NodeStatuses.Should().NotBeNull();
        _context.NodeTraffics.Should().NotBeNull();
        _context.Admins.Should().NotBeNull();
        _context.AdminAuditLogs.Should().NotBeNull();
    }

    // ============================================================
    // 用户 CRUD
    // ============================================================
    [Fact]
    public async Task UserCrud_CreateReadUpdateSoftDelete()
    {
        // Create
        var user = new User
        {
            Username = "crud_user", Password = "hashed", Email = "crud@test.com",
            TotalTrafficBytes = 1024, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        user.Id.Should().BeGreaterThan(0);

        // Read
        var read = await _context.Users.FindAsync(user.Id);
        read.Should().NotBeNull();
        read!.Username.Should().Be("crud_user");

        // Update
        read.Email = "new_email@test.com";
        await _context.SaveChangesAsync();
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.Email.Should().Be("new_email@test.com");

        // Soft delete
        updated.IsActive = false;
        await _context.SaveChangesAsync();
        var deleted = await _context.Users.FindAsync(user.Id);
        deleted!.IsActive.Should().BeFalse();
    }

    // ============================================================
    // 审计日志不可修改检测
    // ============================================================
    [Fact]
    public async Task AdminAuditLog_CannotBeModified_ThrowsException()
    {
        var log = new AdminAuditLog
        {
            AdminId = 1, Action = "create", TargetType = "user",
            TargetId = "1", ClientIp = "127.0.0.1", CreatedAt = DateTime.UtcNow
        };
        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync();

        log.Action = "update";

        var act = () => _context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AdminAuditLogs*");
    }

    // ============================================================
    // 流量记录创建和查询
    // ============================================================
    [Fact]
    public async Task TrafficRecord_CreateAndQuery()
    {
        var user = new User { Id = 1, Username = "traffic_test", Password = "h", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var record = new TrafficRecord
        {
            UserId = 1, BytesIn = 500, BytesOut = 1000,
            NodeId = "node-01", IdempotencyKey = "unique_key_001", RecordedAt = DateTime.UtcNow
        };
        _context.TrafficRecords.Add(record);
        await _context.SaveChangesAsync();

        var records = await _context.TrafficRecords
            .Where(r => r.UserId == 1).ToListAsync();
        records.Should().ContainSingle();
        records[0].BytesIn.Should().Be(500);
        records[0].BytesOut.Should().Be(1000);
    }

    // ============================================================
    // 会话生命周期
    // ============================================================
    [Fact]
    public async Task Session_CreateAndUpdateStatus()
    {
        var user = new User { Id = 1, Username = "sess_user", Password = "h", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = 1, NodeId = "node-01", SessionKey = "key_001",
            StartedAt = DateTime.UtcNow, Status = "active", IdleCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Transition: active → idle → closed
        session.Status = "idle";
        session.IdleCount = 1;
        await _context.SaveChangesAsync();

        session.IdleCount = 3;
        session.Status = "closed";
        session.EndedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var closed = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionKey == "key_001");
        closed!.Status.Should().Be("closed");
        closed.EndedAt.Should().NotBeNull();
    }
}
