using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// 数据保留策略服务 — Phase 3 新增。
/// 每日执行一次清理任务，删除超过保留期限的历史数据。
/// </summary>
public class DataRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionService> _logger;
    private readonly int _trafficRetentionDays;
    private readonly int _authLogRetentionDays;
    private readonly int _nodeStatusRetentionDays;
    private readonly int _nodeTrafficRetentionDays;
    private readonly int _adminAuditLogRetentionDays;
    private readonly int _sessionRetentionDays;

    public DataRetentionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DataRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _trafficRetentionDays = configuration.GetValue<int>("Traffic:TrafficDataRetentionDays", 90);
        _authLogRetentionDays = configuration.GetValue<int>("Traffic:AuthLogRetentionDays", 90);
        _nodeStatusRetentionDays = configuration.GetValue<int>("Node:StatusRetentionDays", 30);
        _nodeTrafficRetentionDays = configuration.GetValue<int>("Traffic:TrafficDataRetentionDays", 90);
        _adminAuditLogRetentionDays = configuration.GetValue<int>("Admin:AuditLogRetentionDays", 365);
        _sessionRetentionDays = configuration.GetValue<int>("Traffic:SessionRetentionDays", 7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("数据保留策略服务已启动（每日清理）");

        while (!stoppingToken.IsCancellationRequested)
        {
            // 等到凌晨 2:00 执行（或立即执行首次 + 之后每 24h）
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2); // 明天凌晨 2:00 UTC
            var delay = nextRun - now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.FromHours(1); // 异常兜底

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据清理任务异常");
            }
        }
    }

    /// <summary>
    /// 执行一次完整的数据清理。
    /// </summary>
    public async Task RunCleanupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("开始执行数据清理...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        int totalDeleted = 0;

        // 1. TrafficRecords — 90 天
        var trafficThreshold = DateTime.UtcNow.AddDays(-_trafficRetentionDays);
        var oldTraffic = await context.TrafficRecords
            .Where(r => r.RecordedAt < trafficThreshold)
            .ToListAsync(ct);
        if (oldTraffic.Count > 0)
        {
            context.TrafficRecords.RemoveRange(oldTraffic);
            totalDeleted += oldTraffic.Count;
            _logger.LogInformation("清理 TrafficRecords: {Count} 条（超过 {Days} 天）", oldTraffic.Count, _trafficRetentionDays);
        }

        // 2. AuthLogs — 90 天
        var authLogThreshold = DateTime.UtcNow.AddDays(-_authLogRetentionDays);
        var oldAuthLogs = await context.AuthLogs
            .Where(l => l.AuthTime < authLogThreshold)
            .ToListAsync(ct);
        if (oldAuthLogs.Count > 0)
        {
            context.AuthLogs.RemoveRange(oldAuthLogs);
            totalDeleted += oldAuthLogs.Count;
            _logger.LogInformation("清理 AuthLogs: {Count} 条（超过 {Days} 天）", oldAuthLogs.Count, _authLogRetentionDays);
        }

        // 3. NodeStatus — 30 天
        var nodeStatusThreshold = DateTime.UtcNow.AddDays(-_nodeStatusRetentionDays);
        var oldNodeStatus = await context.NodeStatuses
            .Where(s => s.ReportedAt < nodeStatusThreshold)
            .ToListAsync(ct);
        if (oldNodeStatus.Count > 0)
        {
            context.NodeStatuses.RemoveRange(oldNodeStatus);
            totalDeleted += oldNodeStatus.Count;
            _logger.LogInformation("清理 NodeStatus: {Count} 条（超过 {Days} 天）", oldNodeStatus.Count, _nodeStatusRetentionDays);
        }

        // 4. NodeTraffic — 90 天
        var nodeTrafficThreshold = DateTime.UtcNow.AddDays(-_nodeTrafficRetentionDays);
        var oldNodeTraffic = await context.NodeTraffics
            .Where(t => t.RecordedAt < nodeTrafficThreshold)
            .ToListAsync(ct);
        if (oldNodeTraffic.Count > 0)
        {
            context.NodeTraffics.RemoveRange(oldNodeTraffic);
            totalDeleted += oldNodeTraffic.Count;
            _logger.LogInformation("清理 NodeTraffic: {Count} 条（超过 {Days} 天）", oldNodeTraffic.Count, _nodeTrafficRetentionDays);
        }

        // 5. AdminAuditLogs — 365 天
        var auditLogThreshold = DateTime.UtcNow.AddDays(-_adminAuditLogRetentionDays);
        var oldAuditLogs = await context.AdminAuditLogs
            .Where(l => l.CreatedAt < auditLogThreshold)
            .ToListAsync(ct);
        if (oldAuditLogs.Count > 0)
        {
            context.AdminAuditLogs.RemoveRange(oldAuditLogs);
            totalDeleted += oldAuditLogs.Count;
            _logger.LogInformation("清理 AdminAuditLogs: {Count} 条（超过 {Days} 天）", oldAuditLogs.Count, _adminAuditLogRetentionDays);
        }

        // 6. Sessions (closed) — 7 天（CleanupClosedSessionsAsync 内部已有 SaveChanges）
        var sessionThreshold = DateTime.UtcNow.AddDays(-_sessionRetentionDays);
        var deletedSessions = await sessionRepo.CleanupClosedSessionsAsync(sessionThreshold);
        totalDeleted += deletedSessions;
        if (deletedSessions > 0)
            _logger.LogInformation("清理 Sessions (closed): {Count} 条（超过 {Days} 天）", deletedSessions, _sessionRetentionDays);

        // 提交所有非 Session 的清理结果（Session 已由 CleanupClosedSessionsAsync 提交）
        if (totalDeleted - deletedSessions > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("数据清理完成，共删除 {Total} 条记录", totalDeleted);
    }
}
