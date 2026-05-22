using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// Edge Agent 流量采集模块 — Phase 3 核心交付。
/// 定时调用本地 Hysteria trafficStats API:
///   GET /traffic?clear=1 — 获取各用户流量（采集后 Hysteria 内部清零）
///   GET /online — 获取各用户在线连接数
/// 采集结果缓存在内存中，等待下一次心跳合并发送。
/// </summary>
public class TrafficCollector : BackgroundService
{
    private readonly AgentConfig _config;
    private readonly StatusReporter _reporter;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TrafficCollector> _logger;

    private TrafficData? _lastTrafficData;
    private int _consecutiveFailures;

    public TrafficCollector(
        AgentConfig config,
        StatusReporter reporter,
        IHttpClientFactory httpClientFactory,
        ILogger<TrafficCollector> logger)
    {
        _config = config;
        _reporter = reporter;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("流量采集服务已启动，间隔: {Interval}s",
            _config.TrafficStats.CollectIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_config.TrafficStats.CollectIntervalSeconds),
                    stoppingToken);

                await CollectAsync(stoppingToken);
                _consecutiveFailures = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                _consecutiveFailures++;
                _logger.LogWarning(ex, "流量采集 HTTP 失败 ({Consecutive}/3 连续失败)", _consecutiveFailures);
                if (_consecutiveFailures >= 3)
                    _logger.LogError("流量采集连续失败 3 次，Hysteria trafficStats API 可能不可达");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流量采集异常");
            }
        }
    }

    /// <summary>
    /// 执行一次流量采集：调用 Hysteria /traffic?clear=1 和 /online，
    /// 将结果缓存并传递给 StatusReporter 以备下次心跳合并。
    /// </summary>
    private async Task CollectAsync(CancellationToken ct)
    {
        var baseUrl = $"http://{_config.TrafficStats.ListenAddress}:{_config.TrafficStats.ListenPort}";
        var client = _httpClientFactory.CreateClient("TrafficStats");
        client.BaseAddress = new Uri(baseUrl);

        // 1. 获取流量（?clear=1 确保采集后 Hysteria 内部清零 → 每次都拿到增量数据）
        var trafficUrl = $"/traffic?clear=1&secret={Uri.EscapeDataString(_config.TrafficStats.Secret)}";
        var trafficResponse = await client.GetAsync(trafficUrl, ct);

        Dictionary<string, UserTrafficEntry>? traffic = null;
        if (trafficResponse.IsSuccessStatusCode)
        {
            traffic = await trafficResponse.Content.ReadFromJsonAsync<Dictionary<string, UserTrafficEntry>>(
                cancellationToken: ct);
        }
        else
        {
            _logger.LogWarning("GET /traffic 返回 HTTP {StatusCode}", (int)trafficResponse.StatusCode);
        }

        // 2. 获取在线用户
        var onlineUrl = $"/online?secret={Uri.EscapeDataString(_config.TrafficStats.Secret)}";
        var onlineResponse = await client.GetAsync(onlineUrl, ct);

        Dictionary<string, int>? online = null;
        if (onlineResponse.IsSuccessStatusCode)
        {
            online = await onlineResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>(
                cancellationToken: ct);
        }
        else
        {
            _logger.LogWarning("GET /online 返回 HTTP {StatusCode}", (int)onlineResponse.StatusCode);
        }

        // 3. 缓存并传递给 StatusReporter
        _lastTrafficData = new TrafficData
        {
            UserTraffic = traffic ?? new Dictionary<string, UserTrafficEntry>(),
            OnlineUsers = online ?? new Dictionary<string, int>(),
            CollectedAt = DateTime.UtcNow
        };

        _reporter.SetTrafficData(_lastTrafficData);

        _logger.LogDebug("流量采集完成: {TrafficCount} 用户流量, {OnlineCount} 在线",
            traffic?.Count ?? 0, online?.Count ?? 0);
    }
}

/// <summary>
/// 流量采集结果缓存
/// </summary>
public class TrafficData
{
    public Dictionary<string, UserTrafficEntry> UserTraffic { get; set; } = new();
    public Dictionary<string, int> OnlineUsers { get; set; } = new();
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
