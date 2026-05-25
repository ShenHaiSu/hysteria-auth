using System.Diagnostics;
using System.Text.Json;
using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// 边缘节点初始化注册模块。
/// 负责启动时的令牌比对、向主服务器注册、配置重建等初始化流程。
/// Phase 7: 支持从注册响应中提取 ConfigYaml 写入 Hysteria 2 配置文件并应用 iptables 端口跳跃规则。
/// </summary>
public class Initializer
{
    private readonly string _configPath;
    private readonly ILogger<Initializer> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Hysteria 2 服务端配置文件路径（可通过 --hysteria-config 参数覆盖）。
    /// </summary>
    public string HysteriaConfigPath { get; set; } = "/etc/hysteria/config.yaml";

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
        var cliHysteriaConfig = ParseCliArg(args, "--hysteria-config");
        var localProvisionToken = localConfig?.ProvisionToken;

        // 2. 如果命令行指定了 Hysteria 配置路径，覆盖默认值
        if (cliHysteriaConfig != null)
            HysteriaConfigPath = cliHysteriaConfig;

        // 3. 确定 MasterServerUrl
        var masterUrl = cliMasterUrl ?? localConfig?.MasterServerUrl ?? "https://master.example.com";

        // 4. 令牌比对
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

            // Phase 7: 尝试从主服务器同步最新配置
            _ = SyncConfigFromMasterAsync();

            return true;
        }

        _logger.LogError("无有效配置，无法启动。请通过 --provision-token 参数提供预注册令牌");
        return false;
    }

    /// <summary>
    /// 使用预注册令牌向主服务器注册。
    /// Phase 7: 注册成功后保存 ConfigYaml 并应用 iptables 规则。
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

                // Phase 7: 如果返回了 ConfigYaml，写入 Hysteria 2 配置文件
                if (!string.IsNullOrWhiteSpace(result.ConfigYaml))
                {
                    await WriteHysteriaConfigAsync(result.ConfigYaml);
                }

                // Phase 7: 应用 iptables 端口跳跃规则
                // 从 ConfigYaml 或默认配置中推断端口跳跃设置
                ApplyPortHoppingRulesIfNeeded(result);

                // 生成并持久化新配置
                var newConfig = new AgentConfig
                {
                    NodeId = result.NodeId,
                    NodeName = $"Edge-{result.NodeId}",
                    MasterServerUrl = masterUrl,
                    NodeSecret = result.NodeSecret,
                    AgentVersion = "1.0.0",
                    ConfigVersion = result.ConfigVersion,
                    ConfigYamlPath = HysteriaConfigPath,
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

                _logger.LogInformation("节点注册成功: {NodeId}, 配置版本: {Version}", result.NodeId, result.ConfigVersion);

                // Phase 7: 重载 Hysteria 2 服务
                await ReloadHysteriaServiceAsync();

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
    // Phase 7: 配置同步
    // ============================

    /// <summary>
    /// 从主服务器同步最新配置，检测本地版本是否落后。
    /// </summary>
    public async Task SyncConfigFromMasterAsync()
    {
        try
        {
            var requestMsg = new HttpRequestMessage(HttpMethod.Get,
                $"{Config.MasterServerUrl}/api/v1/nodes/{Config.NodeId}/config");
            requestMsg.Headers.Add("X-Node-Secret", Config.NodeSecret);

            var response = await _httpClient.SendAsync(requestMsg);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("配置同步失败 (HTTP {Code})", (int)response.StatusCode);
                return;
            }

            var configResponse = await response.Content.ReadFromJsonAsync<RegisterWithTokenResponse>();
            if (configResponse == null) return;

            // 检查配置版本是否更新
            if (configResponse.ConfigVersion > Config.ConfigVersion)
            {
                _logger.LogInformation("检测到配置变更 (本地: {Local}, 远程: {Remote})，更新配置文件",
                    Config.ConfigVersion, configResponse.ConfigVersion);

                if (!string.IsNullOrWhiteSpace(configResponse.ConfigYaml))
                {
                    await WriteHysteriaConfigAsync(configResponse.ConfigYaml);
                }

                Config.ConfigVersion = configResponse.ConfigVersion;
                SaveConfig(Config);

                // 重载 Hysteria 服务
                await ReloadHysteriaServiceAsync();
            }
            else
            {
                _logger.LogDebug("配置版本一致 ({Version})，无需更新", Config.ConfigVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "配置同步异常");
        }
    }

    /// <summary>
    /// 将 ConfigYaml 写入 Hysteria 2 配置文件。
    /// </summary>
    private async Task WriteHysteriaConfigAsync(string configYaml)
    {
        try
        {
            var dir = Path.GetDirectoryName(HysteriaConfigPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(HysteriaConfigPath, configYaml);
            _logger.LogInformation("Hysteria 2 配置文件已写入: {Path}", HysteriaConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入 Hysteria 2 配置文件失败: {Path}", HysteriaConfigPath);
        }
    }

    /// <summary>
    /// 根据注册响应应用 iptables 端口跳跃规则（幂等操作）。
    /// </summary>
    private void ApplyPortHoppingRulesIfNeeded(RegisterWithTokenResponse response)
    {
        // 端口跳跃相关参数从 ConfigYaml 或默认值推断
        // 默认: listenPort=6789, rangeStart=61000, rangeEnd=63000
        var listenPort = 6789;
        var rangeStart = 61000;
        var rangeEnd = 63000;

        // 尝试从 ConfigYaml 解析端口跳跃设置
        if (!string.IsNullOrWhiteSpace(response.ConfigYaml))
        {
            var lines = response.ConfigYaml.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("listen:"))
                {
                    // 解析 listen: 0.0.0.0:6789,0.0.0.0:61000-63000
                    var parts = trimmed["listen:".Length..].Trim().Split(',');
                    if (parts.Length >= 2)
                    {
                        // 第一个部分获取 listenPort
                        var firstPart = parts[0].Trim();
                        var colonIdx = firstPart.LastIndexOf(':');
                        if (colonIdx >= 0 && int.TryParse(firstPart[(colonIdx + 1)..], out var port))
                            listenPort = port;

                        // 第二个部分获取范围
                        var secondPart = parts[1].Trim();
                        var rangeStartColon = secondPart.LastIndexOf(':');
                        if (rangeStartColon >= 0)
                        {
                            var rangeStr = secondPart[(rangeStartColon + 1)..];
                            var dashIdx = rangeStr.IndexOf('-');
                            if (dashIdx >= 0)
                            {
                                if (int.TryParse(rangeStr[..dashIdx], out var start))
                                    rangeStart = start;
                                if (int.TryParse(rangeStr[(dashIdx + 1)..], out var end))
                                    rangeEnd = end;
                            }
                        }
                    }
                    else
                    {
                        // 单端口，无端口跳跃
                        _logger.LogDebug("未启用端口跳跃，跳过 iptables 规则");
                        return;
                    }
                    break;
                }
            }
        }

        ApplyPortHoppingRules(listenPort, rangeStart, rangeEnd);
    }

    /// <summary>
    /// 应用 iptables NAT 规则实现端口跳跃（幂等：已存在则跳过）。
    /// </summary>
    private void ApplyPortHoppingRules(int listenPort, int rangeStart, int rangeEnd)
    {
        try
        {
            // 检查 iptables 是否可用
            var whichResult = RunCommand("which", "iptables");
            if (whichResult.ExitCode != 0)
            {
                _logger.LogDebug("iptables 不可用，跳过端口跳跃规则设置");
                return;
            }

            // 检测默认网络接口
            var iface = DetectDefaultInterface() ?? "eth0";

            // 检查规则是否已存在（幂等）
            var checkCmd = $"iptables -t nat -C PREROUTING -i {iface} -p udp --dport {rangeStart}:{rangeEnd} -j DNAT --to-destination :{listenPort} 2>/dev/null";
            var checkResult = RunCommand("bash", $"-c \"{checkCmd}\"");

            if (checkResult.ExitCode != 0)
            {
                // 规则不存在，添加
                var addCmd = $"iptables -t nat -A PREROUTING -i {iface} -p udp --dport {rangeStart}:{rangeEnd} -j DNAT --to-destination :{listenPort}";
                var addResult = RunCommand("bash", $"-c \"{addCmd}\"");

                if (addResult.ExitCode == 0)
                {
                    _logger.LogInformation(
                        "iptables 端口跳跃规则已添加: {Iface} UDP {Start}:{End} → :{Port}",
                        iface, rangeStart, rangeEnd, listenPort);
                }
                else
                {
                    _logger.LogWarning(
                        "iptables 端口跳跃规则添加失败: {Error}", addResult.Stderr);
                }
            }
            else
            {
                _logger.LogDebug("iptables 端口跳跃规则已存在，跳过");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "应用 iptables 端口跳跃规则时发生异常");
        }
    }

    /// <summary>
    /// 重载 Hysteria 2 服务（通过 systemctl）。
    /// </summary>
    private async Task ReloadHysteriaServiceAsync()
    {
        try
        {
            var result = RunCommand("systemctl", "reload hysteria-server.service 2>/dev/null || systemctl restart hysteria-server.service 2>/dev/null || true");
            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Hysteria 2 服务已重载");
            }
            else
            {
                _logger.LogDebug("Hysteria 2 服务重载命令未执行（可能 systemctl 不可用）");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "重载 Hysteria 2 服务失败");
        }
    }

    // ============================
    // 系统工具方法
    // ============================

    /// <summary>
    /// 检测默认网络接口。
    /// </summary>
    private static string? DetectDefaultInterface()
    {
        try
        {
            var result = RunCommand("bash", "-c \"ip route show default 2>/dev/null | awk '{print $5}' | head -1\"");
            var iface = result.Stdout?.Trim();
            if (!string.IsNullOrWhiteSpace(iface))
                return iface;
        }
        catch
        {
            // 忽略
        }
        return null;
    }

    /// <summary>
    /// 执行系统命令，返回退出码、stdout 和 stderr。
    /// </summary>
    private static CommandResult RunCommand(string command, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(5000); // 5 秒超时

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                Stdout = stdout,
                Stderr = stderr
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                ExitCode = -1,
                Stderr = ex.Message
            };
        }
    }

    private record CommandResult
    {
        public int ExitCode { get; init; }
        public string Stdout { get; init; } = string.Empty;
        public string Stderr { get; init; } = string.Empty;
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
