using System.Text.Json.Serialization;

namespace HysteriaAuth.Master.Models.DTOs;

// ============================
// 节点预注册（管理员侧）
// ============================

public class PreRegisterNodeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Port { get; set; } = 443;
    public int? TrafficStatsPort { get; set; }
}

public class PreRegisterNodeResponse
{
    public string ProvisionToken { get; set; } = string.Empty;
    public string MasterServerUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string StartupCommand { get; set; } = string.Empty;
}

// ============================
// 令牌注册（Edge Agent 侧）
// ============================

public class RegisterWithTokenRequest
{
    public string ProvisionToken { get; set; } = string.Empty;
    public string? NodeId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? AgentVersion { get; set; }
    public string? Name { get; set; }
}

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
// 旧版注册（保留兼容）
// ============================

public class RegisterNodeRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public string? Location { get; set; }
    public int? TrafficStatsPort { get; set; }
    public string? TrafficStatsSecret { get; set; }
}

// ============================
// 节点配置同步
// ============================

public class NodeConfigResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public NodeConfigInfo Config { get; set; } = new();
}

// ============================
// 心跳请求
// ============================

public class HeartbeatRequest
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

    /// <summary>Phase 2 为空对象，Phase 3 填充真实流量数据</summary>
    public Dictionary<string, UserTrafficEntry> UserTraffic { get; set; } = new();

    /// <summary>Phase 2 为空对象，Phase 3 填充真实在线用户数据</summary>
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
// 节点 DTO（列表/详情返回）
// ============================

public class NodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string? Location { get; set; }
    public int? TrafficStatsPort { get; set; }
    public string ProvisionStatus { get; set; } = string.Empty;
}

public class NodeListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<NodeDto> Items { get; set; } = new();
}

public class NodeDetailResponse : NodeDto
{
    public int SecretVersion { get; set; }
    public string? TrafficStatsSecret { get; set; }
}

// ============================
// 节点历史状态
// ============================

public class NodeStatusDto
{
    public long Id { get; set; }
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
}

public class NodeStatusHistoryResponse
{
    public string NodeId { get; set; } = string.Empty;
    public int Hours { get; set; }
    public List<NodeStatusDto> Items { get; set; } = new();
}

// ============================
// 密钥轮换
// ============================

public class RotateSecretResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NewSecret { get; set; } = string.Empty;
    public int NewSecretVersion { get; set; }
}

// ============================
// 踢用户下线
// ============================

public class KickUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
}

// ============================
// 用户流量统计 (Phase 3)
// ============================

public class UserTrafficStatsResponse
{
    public long UserId { get; set; }
    public string Period { get; set; } = string.Empty;
    public long TotalBytesIn { get; set; }
    public long TotalBytesOut { get; set; }
    public List<TrafficDataPoint> DataPoints { get; set; } = new();
}

public class TrafficDataPoint
{
    public DateTime Date { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
}
