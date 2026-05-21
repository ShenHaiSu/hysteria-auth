using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

/// <summary>
/// 流量记录仓储实现 — Phase 3 新增。
/// </summary>
public class TrafficRepository : ITrafficRepository
{
    private readonly AppDbContext _context;

    public TrafficRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TrafficRecord> AddTrafficRecordAsync(TrafficRecord record)
    {
        _context.TrafficRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<bool> CheckIdempotencyKeyExistsAsync(string idempotencyKey)
    {
        return await _context.TrafficRecords
            .AnyAsync(r => r.IdempotencyKey == idempotencyKey);
    }

    public async Task<List<TrafficRecord>> GetUserTrafficStatsAsync(long userId, DateTime start, DateTime end)
    {
        return await _context.TrafficRecords
            .Where(r => r.UserId == userId && r.RecordedAt >= start && r.RecordedAt <= end)
            .OrderBy(r => r.RecordedAt)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<TrafficRecord> records)
    {
        _context.TrafficRecords.AddRange(records);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// 会话仓储实现 — Phase 3 新增。
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _context;

    public SessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Session>> GetActiveSessionsByNodeAsync(string nodeId)
    {
        return await _context.Sessions
            .Where(s => s.NodeId == nodeId
                && (s.Status == "active" || s.Status == "idle"))
            .ToListAsync();
    }

    public async Task AddSessionAsync(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSessionsAsync(IEnumerable<Session> sessions)
    {
        _context.Sessions.UpdateRange(sessions);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CleanupClosedSessionsAsync(DateTime threshold)
    {
        var expired = await _context.Sessions
            .Where(s => s.Status == "closed" && s.EndedAt < threshold)
            .ToListAsync();

        if (expired.Count == 0) return 0;

        _context.Sessions.RemoveRange(expired);
        return await _context.SaveChangesAsync();
    }

    public async Task<List<Session>> GetExpiredSessionsAsync(DateTime threshold)
    {
        return await _context.Sessions
            .Where(s => s.Status == "closed" && s.EndedAt < threshold)
            .ToListAsync();
    }
}
