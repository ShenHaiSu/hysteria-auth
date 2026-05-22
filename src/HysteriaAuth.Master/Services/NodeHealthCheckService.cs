using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// 节点离线检测后台服务。
/// 定期扫描心跳超时的节点并将其标记为离线（IsActive = false）。
/// </summary>
public class NodeHealthCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NodeHealthCheckService> _logger;
    private readonly int _heartbeatTimeoutSeconds;
    private readonly int _checkIntervalSeconds;

    public NodeHealthCheckService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<NodeHealthCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeatTimeoutSeconds = configuration.GetValue<int>("Node:HeartbeatTimeoutSeconds", 90);
        // 每 timeout/3 秒检查一次（默认 30s）
        _checkIntervalSeconds = _heartbeatTimeoutSeconds / 3;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("节点健康检查服务已启动，超时阈值: {Timeout}s，检查间隔: {Interval}s",
            _heartbeatTimeoutSeconds, _checkIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var threshold = DateTime.UtcNow.AddSeconds(-_heartbeatTimeoutSeconds);

                // 查找所有活跃但心跳超时的节点
                var offlineNodes = await context.Nodes
                    .Where(n => n.IsActive && n.LastHeartbeat < threshold)
                    .ToListAsync(stoppingToken);

                foreach (var node in offlineNodes)
                {
                    node.IsActive = false;
                    _logger.LogWarning("节点 {NodeId}（{Name}）心跳超时（>{Timeout}s），标记为离线",
                        node.Id, node.Name, _heartbeatTimeoutSeconds);
                }

                if (offlineNodes.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "节点健康检查执行异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), stoppingToken);
        }
    }
}
