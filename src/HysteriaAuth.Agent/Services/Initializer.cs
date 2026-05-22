using System.Text.Json;
using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// 边缘节点初始化注册模块。
/// 负责启动时的令牌比对、向主服务器注册、配置重建等初始化流程。
/// </summary>
public class Initializer
{
    private readonly string _configPath;
    private readonly ILogger<Initializer> _logger;
    private readonly HttpClient _httpClient;

    public AgentConfig Config { get; private set; } = new();

    public Initializer(string configPath, ILogger<Initializer> logger)
    {
        _configPath = configPath;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 执行启动初始化序列：加载配置 → 令牌比对 → 注册/配置同步 → 返回最终配置。
    /// </summary>
    public async Task<bool> InitializeAsync(string[] args)
    {
        // 1. 加载本地配置
        var localConfig = LoadConfig();
        var cliProvisionToken = ParseCliArg(args, "--provision-token");
        var cliMasterUrl = ParseCliArg(args, "--master-url");
        var localProvisionToken = localConfig?.ProvisionToken;

        // 2. 确定 MasterServerUrl
        var masterUrl = cliMasterUrl ?? localConfig?.MasterServerUrl ?? "https://master.example.com";

        // 3. 令牌比对
        var shouldReregister = cliProvisionToken != null
            && cliProvisionToken != localProvisionToken;

        if (shouldReregister)
        {
            _logger.LogInformation("检测到令牌变更，将使用新令牌重新注册");
            BackupConfig();
            DeleteConfig();
            return await RegisterWithTokenAsync(cliProvisionToken!, masterUrl);
        }

        if (localProvisionToken != null)
        {
            _logger.LogInformation("使用本地配置中的令牌注册");
            return await RegisterWithTokenAsync(localProvisionToken, masterUrl);
        }

        if (localConfig?.NodeId != null && !string.IsNullOrWhiteSpace(localConfig.NodeSecret))
        {
            _logger.LogInformation("使用已有身份启动: {NodeId}", localConfig.NodeId);
            Config = localConfig;
            Config.MasterServerUrl = masterUrl;
            return true;
        }

        _logger.LogError("无有效配置，无法启动。请通过 --provision-token 参数提供预注册令牌");
        return false;
    }

    /// <summary>
    /// 使用预注册令牌向主服务器注册。
    /// </summary>
    private async Task<bool> RegisterWithTokenAsync(string token, string masterUrl)
    {
        var maxRetries = 5;
        var initialDelay = 2;
        var maxDelay = 60;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("正在向主服务器注册 ({Attempt}/{Max})...", attempt + 1, maxRetries);

                var payload = new
                {
                    provisionToken = token,
                    ipAddress = GetLocalIpAddress(),
                    agentVersion = "1.0.0"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{masterUrl}/api/v1/nodes/register-with-token", content);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("注册失败 (HTTP {Code}): {Body}", (int)response.StatusCode, body);

                    if (attempt < maxRetries - 1)
                    {
                        var delay = Math.Min(initialDelay * Math.Pow(2, attempt), maxDelay);
                        _logger.LogInformation("{Delay}s 后重试...", delay);
                        await Task.Delay(TimeSpan.FromSeconds(delay));
                        continue;
                    }

                    _logger.LogCritical("注册失败，已达最大重试次数");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<RegisterWithTokenResponse>();
                if (result == null)
                {
                    _logger.LogError("注册响应解析失败");
                    return false;
                }

                // 生成并持久化新配置
                var newConfig = new AgentConfig
                {
                    NodeId = result.NodeId,
                    NodeName = $"Edge-{result.NodeId}",
                    MasterServerUrl = masterUrl,
                    NodeSecret = result.NodeSecret,
                    AgentVersion = "1.0.0",
                    AuthProxy = new AgentAuthProxyConfig
                    {
                        ListenAddress = "127.0.0.1",
                        ListenPort = result.Config.AuthProxyPort
                    },
                    TrafficStats = new TrafficStatsConfig
                    {
                        ListenAddress = "127.0.0.1",
                        ListenPort = result.Config.TrafficStatsPort,
                        Secret = result.TrafficStatsSecret,
                        CollectIntervalSeconds = result.Config.CollectIntervalSeconds
                    },
                    Monitor = new MonitorConfig
                    {
                        IntervalSeconds = 10,
                        NetworkInterfaces = new List<string> { "eth0" }
                    },
                    Reporter = new ReporterConfig
                    {
                        IntervalSeconds = result.Config.HeartbeatIntervalSeconds,
                        RetryCount = 3,
                        RetryDelaySeconds = 5
                    },
                    Cache = new CacheConfig
                    {
                        Enabled = true,
                        MaxSize = 1000,
                        ExpirationMinutes = 5
                    },
                    HealthCheck = new HealthCheckConfig
                    {
                        Enabled = true,
                        ListenAddress = "127.0.0.1",
                        ListenPort = result.Config.HealthCheckPort
                    },
                    Logging = new AgentLoggingConfig
                    {
                        LogLevel = "Information"
                    }
                };

                SaveConfig(newConfig);
                Config = newConfig;

                _logger.LogInformation("节点注册成功: {NodeId}", result.NodeId);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "网络请求失败 ({Attempt}/{Max})", attempt + 1, maxRetries);
                if (attempt < maxRetries - 1)
                {
                    var delay = Math.Min(initialDelay * Math.Pow(2, attempt), maxDelay);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册过程中发生未预期异常");
                if (attempt >= maxRetries - 1) return false;
                var delay = Math.Min(initialDelay * Math.Pow(2, attempt), maxDelay);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        _logger.LogCritical("注册失败，已达最大重试次数");
        return false;
    }

    // ============================
    // 配置管理
    // ============================

    private AgentConfig? LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            _logger.LogDebug("配置文件不存在: {Path}", _configPath);
            return null;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AgentConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "配置文件读取失败: {Path}", _configPath);
            return null;
        }
    }

    private void SaveConfig(AgentConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(_configPath, json);
        _logger.LogInformation("配置已保存: {Path}", _configPath);
    }

    private void BackupConfig()
    {
        if (!File.Exists(_configPath)) return;

        var backupPath = $"{_configPath}.bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
        File.Copy(_configPath, backupPath);
        _logger.LogInformation("旧配置已备份: {Path}", backupPath);
    }

    private void DeleteConfig()
    {
        if (File.Exists(_configPath))
        {
            File.Delete(_configPath);
            _logger.LogInformation("旧配置已删除: {Path}", _configPath);
        }
    }

    // ============================
    // CLI 参数解析
    // ============================

    private static string? ParseCliArg(string[] args, string argName)
    {
        var prefix = $"{argName}=";
        var match = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (match == null) return null;
        return match[prefix.Length..];
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = System.Net.Dns.GetHostAddresses(hostName);
            var ipv4 = addresses.FirstOrDefault(a =>
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !System.Net.IPAddress.IsLoopback(a));
            return ipv4?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
