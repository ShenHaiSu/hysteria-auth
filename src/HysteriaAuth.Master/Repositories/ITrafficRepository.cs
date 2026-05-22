using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

/// <summary>
/// 流量记录仓储接口 — Phase 3 新增。
/// </summary>
public interface ITrafficRepository
{
    Task<TrafficRecord> AddTrafficRecordAsync(TrafficRecord record);
    Task<bool> CheckIdempotencyKeyExistsAsync(string idempotencyKey);
    Task<List<TrafficRecord>> GetUserTrafficStatsAsync(long userId, DateTime start, DateTime end);
    Task AddRangeAsync(IEnumerable<TrafficRecord> records);
}

/// <summary>
/// 会话仓储接口 — Phase 3 新增。
/// </summary>
public interface ISessionRepository
{
    Task<List<Session>> GetActiveSessionsByNodeAsync(string nodeId);
    Task AddSessionAsync(Session session);
    Task UpdateSessionsAsync(IEnumerable<Session> sessions);
    Task<int> CleanupClosedSessionsAsync(DateTime threshold);
    Task<List<Session>> GetExpiredSessionsAsync(DateTime threshold);
}
