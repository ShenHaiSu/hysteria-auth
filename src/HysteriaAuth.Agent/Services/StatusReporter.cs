using System.Net.Http.Json;
using System.Text.Json;
using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// 状态上报模块 — 定时将系统监控数据 + 流量数据通过心跳上报到主服务器。
/// Phase 3: 集成 TrafficCollector 采集的 userTraffic 和 onlineUsers 到心跳中。
/// Phase 7: 心跳响应中检查 configVersion，检测到变更时触发配置同步。
/// </summary>
public class StatusReporter : BackgroundService
{
    private readonly AgentConfig _config;
    private readonly SystemMonitor _systemMonitor;
    private readonly Initializer _initializer;
    private readonly HttpClient _httpClient;
    private readonly ILogger<StatusReporter> _logger;

    // Phase 3: 流量数据（由 TrafficCollector 设置）
    private TrafficData? _pendingTrafficData;
    private readonly object _trafficLock = new();

    // Phase 7: 本地缓存的配置版本
    private int _localConfigVersion;

    public StatusReporter(
        AgentConfig config,
        SystemMonitor systemMonitor,
        Initializer initializer,
        ILogger<StatusReporter> logger)
    {
        _config = config;
        _systemMonitor = systemMonitor;
        _initializer = initializer;
        _httpClient = new HttpClient();
        _logger = logger;
        _localConfigVersion = config.ConfigVersion;
    }

    /// <summary>
    /// 由 TrafficCollector 调用，设置待合并的流量数据。
    /// </summary>
    public void SetTrafficData(TrafficData data)
    {
        lock (_trafficLock)
        {
            _pendingTrafficData = data;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("状态上报服务已启动，间隔: {Interval}s",
            _config.Reporter.IntervalSeconds);

        // 发送首次心跳（含空数据）
        await SendHeartbeatAsync(new SystemMetrics { CollectedAt = DateTime.UtcNow }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.Reporter.IntervalSeconds), stoppingToken);

                var metrics = await _systemMonitor.CollectAsync();
                await SendHeartbeatAsync(metrics, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "心跳上报异常");
            }
        }
    }

    private async Task SendHeartbeatAsync(SystemMetrics metrics, CancellationToken ct)
    {
        // Phase 3: 获取待合并的流量数据
        TrafficData? trafficData;
        lock (_trafficLock)
        {
            trafficData = _pendingTrafficData;
            _pendingTrafficData = null; // 消费后清空
        }

        var payload = new HeartbeatPayload
        {
            NodeId = _config.NodeId,
            CpuUsagePercent = metrics.CpuUsagePercent,
            MemoryUsagePercent = metrics.MemoryUsagePercent,
            MemoryUsedMb = metrics.MemoryUsedMb,
            MemoryTotalMb = metrics.MemoryTotalMb,
            NetworkInBytes = metrics.NetworkInBytes,
            NetworkOutBytes = metrics.NetworkOutBytes,
            NetworkInMbps = metrics.NetworkInMbps,
            NetworkOutMbps = metrics.NetworkOutMbps,
            ActiveConnections = metrics.ActiveConnections,
            ReportedAt = metrics.CollectedAt,
            // Phase 3: 填充流量数据
            UserTraffic = trafficData?.UserTraffic ?? new Dictionary<string, UserTrafficEntry>(),
            OnlineUsers = trafficData?.OnlineUsers ?? new Dictionary<string, int>()
        };

        var maxRetries = _config.Reporter.RetryCount;
        var retryDelay = _config.Reporter.RetryDelaySeconds;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Post,
                    $"{_config.MasterServerUrl}/api/v1/nodes/{_config.NodeId}/heartbeat");

                requestMsg.Headers.Add("X-Node-Secret", _config.NodeSecret);
                requestMsg.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(requestMsg, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("心跳上报成功: CPU={Cpu}%, Mem={Mem}%, 流量用户={TrafficCount}",
                        metrics.CpuUsagePercent, metrics.MemoryUsagePercent,
                        payload.UserTraffic.Count);

                    // Phase 7: 检查心跳响应中的 configVersion
                    await CheckConfigVersionAsync(response);

                    return;
                }

                _logger.LogWarning("心跳上报失败 (HTTP {Code})，第 {Attempt}/{Max} 次尝试",
                    (int)response.StatusCode, attempt + 1, maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "心跳上报网络异常，第 {Attempt}/{Max} 次尝试",
                    attempt + 1, maxRetries);
            }

            if (attempt < maxRetries - 1)
                await Task.Delay(TimeSpan.FromSeconds(retryDelay), ct);
        }

        _logger.LogError("心跳上报最终失败，已尝试 {Max} 次", maxRetries);
    }

    /// <summary>
    /// Phase 7: 检查心跳响应中的 configVersion，如果远程版本更新则触发配置同步。
    /// </summary>
    private async Task CheckConfigVersionAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return;

            var heartbeatResponse = JsonSerializer.Deserialize<HeartbeatResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (heartbeatResponse == null) return;

            if (heartbeatResponse.ConfigVersion > _localConfigVersion)
            {
                _logger.LogInformation(
                    "检测到配置变更 (本地: {Local}, 远程: {Remote})，触发配置同步",
                    _localConfigVersion, heartbeatResponse.ConfigVersion);

                await _initializer.SyncConfigFromMasterAsync();
                _localConfigVersion = heartbeatResponse.ConfigVersion;
                _config.ConfigVersion = heartbeatResponse.ConfigVersion;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查心跳响应 configVersion 时发生异常（非关键）");
        }
    }
}
