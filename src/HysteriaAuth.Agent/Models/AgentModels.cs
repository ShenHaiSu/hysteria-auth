using System.Text.Json;
using System.Text.Json.Serialization;

namespace HysteriaAuth.Agent.Models;

// ============================
// Hysteria 协议模型
// ============================

public class HysteriaAuthRequest
{
    public string Addr { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public ulong Tx { get; set; }
}

public class HysteriaAuthResponse
{
    public bool Ok { get; set; }
    public string Id { get; set; } = string.Empty;
}

// ============================
// 内部 API 模型
// ============================

public class InternalAuthRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

public class InternalAuthResponse
{
    public bool Success { get; set; }
    public long UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public long RemainingTraffic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// ============================
// 令牌注册模型
// ============================

public class RegisterWithTokenResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public string TrafficStatsSecret { get; set; } = string.Empty;
    public NodeConfigInfo Config { get; set; } = new();
}

public class NodeConfigInfo
{
    public int AuthProxyPort { get; set; } = 8080;
    public int HealthCheckPort { get; set; } = 8081;
    public int TrafficStatsPort { get; set; } = 9999;
    public int CollectIntervalSeconds { get; set; } = 30;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}

// ============================
// 系统指标模型
// ============================

public class SystemMetrics
{
    public float CpuUsagePercent { get; set; }
    public float MemoryUsagePercent { get; set; }
    public float MemoryUsedMb { get; set; }
    public float MemoryTotalMb { get; set; }
    public long NetworkInBytes { get; set; }
    public long NetworkOutBytes { get; set; }
    public float NetworkInMbps { get; set; }
    public float NetworkOutMbps { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

// ============================
// 心跳上报模型
// ============================

public class HeartbeatPayload
{
    public string NodeId { get; set; } = string.Empty;
    public float CpuUsagePercent { get; set; }
    public float MemoryUsagePercent { get; set; }
    public float MemoryUsedMb { get; set; }
    public float MemoryTotalMb { get; set; }
    public long NetworkInBytes { get; set; }
    public long NetworkOutBytes { get; set; }
    public float NetworkInMbps { get; set; }
    public float NetworkOutMbps { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime ReportedAt { get; set; }

    [JsonPropertyName("userTraffic")]
    public Dictionary<string, UserTrafficEntry> UserTraffic { get; set; } = new();

    [JsonPropertyName("onlineUsers")]
    public Dictionary<string, int> OnlineUsers { get; set; } = new();
}

public class UserTrafficEntry
{
    [JsonPropertyName("tx")]
    public long Tx { get; set; }

    [JsonPropertyName("rx")]
    public long Rx { get; set; }
}

// ============================
// Agent 完整配置
// ============================

public class AgentConfig
{
    public string? ProvisionToken { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string MasterServerUrl { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public InitConfig Init { get; set; } = new();
    public AgentAuthProxyConfig AuthProxy { get; set; } = new();
    public TrafficStatsConfig TrafficStats { get; set; } = new();
    public MonitorConfig Monitor { get; set; } = new();
    public ReporterConfig Reporter { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
    public HealthCheckConfig HealthCheck { get; set; } = new();
    public AgentLoggingConfig Logging { get; set; } = new();
}

public class InitConfig
{
    public int RegistrationRetryMax { get; set; } = 5;
    public int RegistrationRetryInitialSeconds { get; set; } = 2;
    public int RegistrationRetryMaxSeconds { get; set; } = 60;
}

public class AgentAuthProxyConfig
{
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int ListenPort { get; set; } = 8080;
}

public class TrafficStatsConfig
{
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int ListenPort { get; set; } = 9999;
    public string Secret { get; set; } = string.Empty;
    public int CollectIntervalSeconds { get; set; } = 30;
}

public class MonitorConfig
{
    public int IntervalSeconds { get; set; } = 10;
    public List<string> NetworkInterfaces { get; set; } = new() { "eth0" };
}

public class ReporterConfig
{
    public int IntervalSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}

public class CacheConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxSize { get; set; } = 1000;
    public int ExpirationMinutes { get; set; } = 5;
}

public class HealthCheckConfig
{
    public bool Enabled { get; set; } = true;
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int ListenPort { get; set; } = 8081;
}

public class AgentLoggingConfig
{
    public string LogLevel { get; set; } = "Information";
    public string? File { get; set; }
}

// ============================
// 踢用户下线请求（Phase 3）
// ============================

public class KickUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
}
