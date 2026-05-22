using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// 流量统计服务 — Phase 3 核心交付。
/// 负责：
///   - 乐观并发控制的流量扣减
///   - 幂等键检查
///   - 会话生命周期管理（active → idle → closed 状态机）
///   - 用户流量统计聚合查询
/// </summary>
public class TrafficService
{
    private readonly ITrafficRepository _trafficRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly IUserRepository _userRepo;
    private readonly AppDbContext _context;
    private readonly ILogger<TrafficService> _logger;
    private readonly int _retryCount;

    public TrafficService(
        ITrafficRepository trafficRepo,
        ISessionRepository sessionRepo,
        IUserRepository userRepo,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<TrafficService> logger)
    {
        _trafficRepo = trafficRepo;
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
        _context = context;
        _logger = logger;
        _retryCount = configuration.GetValue<int>("Traffic:ConcurrentUpdateRetryCount", 3);
    }

    // ============================================================
    // §8.1 乐观并发流量扣减
    // ============================================================

    /// <summary>
    /// 更新用户的已用流量，使用乐观并发控制 + 最多 3 次重试。
    /// 必须在事务中调用，DbContext 由调用方管理。
    /// </summary>
    public async Task UpdateUsedTrafficAsync(long userId, long bytesToAdd, int? retryCountOverride = null)
    {
        var maxRetries = retryCountOverride ?? _retryCount;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                user.UsedTrafficBytes += bytesToAdd;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogDebug("流量扣减成功: UserId={UserId}, Added={Bytes}, NewTotal={Total}",
                    userId, bytesToAdd, user.UsedTrafficBytes);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (i == maxRetries - 1)
                {
                    _logger.LogError("流量扣减轻试 {RetryCount} 次后仍失败: UserId={UserId}",
                        maxRetries, userId);
                    throw;
                }
                await Task.Delay(Random.Shared.Next(10, 50));
                _context.ChangeTracker.Clear(); // 丢弃已追踪实体，重新加载
                _logger.LogDebug("流量扣减并发冲突，重试 ({Attempt}/{Max}): UserId={UserId}",
                    i + 1, maxRetries, userId);
            }
        }
    }

    // ============================================================
    // §8.2 幂等键生成
    // ============================================================

    /// <summary>
    /// 生成幂等键：{nodeId}_{username}_{timestamp_rounded_to_30s}
    /// </summary>
    public static string GenerateIdempotencyKey(string nodeId, string username, DateTime reportedAt)
    {
        // 向下取整到 30 秒间隔
        var roundedSecond = reportedAt.Second - (reportedAt.Second % 30);
        var rounded = new DateTime(
            reportedAt.Year, reportedAt.Month, reportedAt.Day,
            reportedAt.Hour, reportedAt.Minute, roundedSecond,
            DateTimeKind.Utc);

        return $"{nodeId}_{username}_{rounded:yyyy-MM-ddTHH:mm:ssZ}";
    }

    // ============================================================
    // §8.3 会话生命周期管理
    // ============================================================

    /// <summary>
    /// 根据心跳中的 onlineUsers 更新所有相关会话的状态。
    /// 状态机: active ↔ idle → closed
    /// </summary>
    public async Task UpdateSessionsAsync(string nodeId, Dictionary<string, int> onlineUsers)
    {
        // 1. 获取该节点所有 active/idle 会话
        var activeSessions = await _sessionRepo.GetActiveSessionsByNodeAsync(nodeId);

        var onlineUsernames = new HashSet<string>(onlineUsers.Keys);

        foreach (var session in activeSessions)
        {
            var user = await _userRepo.GetByIdAsync(session.UserId);
            if (user == null) continue;

            if (onlineUsernames.Contains(user.Username))
            {
                // 用户仍在线 → active
                if (session.Status == "idle")
                {
                    _logger.LogInformation("会话恢复: {Username} idle → active", user.Username);
                }
                session.Status = "active";
                session.IdleCount = 0;
            }
            else
            {
                // 用户不在线
                if (session.Status == "active")
                {
                    session.Status = "idle";
                    session.IdleCount = 1;
                    _logger.LogDebug("会话进入空闲: UserId={UserId} ({Username})", session.UserId, user.Username);
                }
                else if (session.Status == "idle")
                {
                    session.IdleCount++;
                    if (session.IdleCount >= 3) // 连续 3 次不在线 → 关闭
                    {
                        session.Status = "closed";
                        session.EndedAt = DateTime.UtcNow;
                        _logger.LogInformation("会话关闭: UserId={UserId} ({Username}), 连续 {Count} 次不在线",
                            session.UserId, user.Username, session.IdleCount);
                    }
                }
            }
        }

        // 2. 新建会话：首次出现在在线列表中
        var existingUserIds = activeSessions.Select(s => s.UserId).ToHashSet();
        foreach (var (username, connectionCount) in onlineUsers)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null || existingUserIds.Contains(user.Id)) continue;

            _context.Sessions.Add(new Session
            {
                UserId = user.Id,
                NodeId = nodeId,
                SessionKey = $"{nodeId}_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetHexString(4)}",
                StartedAt = DateTime.UtcNow,
                Status = "active",
                IdleCount = 0
            });

            _logger.LogInformation("新会话创建: UserId={UserId} ({Username}), NodeId={NodeId}, Connections={Conns}",
                user.Id, username, nodeId, connectionCount);
        }
    }

    // ============================================================
    // 用户流量统计聚合查询
    // ============================================================

    /// <summary>
    /// 获取用户流量统计，按指定周期聚合。
    /// period: day | week | month | all
    /// </summary>
    public async Task<UserTrafficStatsResponse> GetUserTrafficStatsAsync(long userId, string period)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("用户不存在");

        var (start, groupBy) = period.ToLower() switch
        {
            "day" => (DateTime.UtcNow.AddHours(-24), TrafficGroupBy.Hour),
            "week" => (DateTime.UtcNow.AddDays(-7), TrafficGroupBy.Day),
            "month" => (DateTime.UtcNow.AddDays(-30), TrafficGroupBy.Day),
            "all" => (DateTime.MinValue, TrafficGroupBy.Day),
            _ => throw new ValidationException("不支持的统计周期，请使用 day/week/month/all")
        };

        var records = await _trafficRepo.GetUserTrafficStatsAsync(userId, start, DateTime.UtcNow);

        // 聚合
        var dataPoints = groupBy switch
        {
            TrafficGroupBy.Hour => records
                .GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, r.RecordedAt.Day, r.RecordedAt.Hour, 0, 0, DateTimeKind.Utc))
                .Select(g => new TrafficDataPoint
                {
                    Date = g.Key,
                    BytesIn = g.Sum(r => r.BytesIn),
                    BytesOut = g.Sum(r => r.BytesOut)
                })
                .OrderBy(d => d.Date)
                .ToList(),
            _ => records
                .GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, r.RecordedAt.Day, 0, 0, 0, DateTimeKind.Utc))
                .Select(g => new TrafficDataPoint
                {
                    Date = g.Key,
                    BytesIn = g.Sum(r => r.BytesIn),
                    BytesOut = g.Sum(r => r.BytesOut)
                })
                .OrderBy(d => d.Date)
                .ToList()
        };

        return new UserTrafficStatsResponse
        {
            UserId = userId,
            Period = period,
            TotalBytesIn = dataPoints.Sum(d => d.BytesIn),
            TotalBytesOut = dataPoints.Sum(d => d.BytesOut),
            DataPoints = dataPoints
        };
    }

    private enum TrafficGroupBy { Hour, Day }
}
