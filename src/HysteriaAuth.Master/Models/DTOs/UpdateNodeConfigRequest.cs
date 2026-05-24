namespace HysteriaAuth.Master.Models.DTOs;

/// <summary>
/// 管理员更新节点配置的请求 DTO。所有字段均为可选（null 表示不修改）。
/// </summary>
public class UpdateNodeConfigRequest
{
    // 监听 & 端口跳跃
    public string? ListenAddress { get; set; }
    public int? ListenPort { get; set; }
    public bool? EnablePortHopping { get; set; }
    public int? PortHopRangeStart { get; set; }
    public int? PortHopRangeEnd { get; set; }

    // 混淆
    public string? ObfsType { get; set; }
    public string? ObfsPassword { get; set; }

    // 拥塞控制
    public string? CongestionControl { get; set; }
    public long? BrutalTxBandwidth { get; set; }

    // QUIC
    public int? QuicMaxIdleTimeout { get; set; }
    public int? QuicMaxUdpPayloadSize { get; set; }

    // 带宽
    public string? BandwidthUp { get; set; }
    public string? BandwidthDown { get; set; }
    public bool? IgnoreClientBandwidth { get; set; }

    // 速度测试
    public bool? EnableSpeedTest { get; set; }
    public int? SpeedTestPingInterval { get; set; }

    // UDP
    public int? UdpIdleTimeout { get; set; }

    // 协议嗅探
    public bool? SniffEnabled { get; set; }
    public int? SniffTimeout { get; set; }
    public bool? SniffRespectHttps { get; set; }

    // 伪装
    public string? MasqueradeType { get; set; }
    public string? MasqueradeFile { get; set; }
    public string? MasqueradeProxyUrl { get; set; }
    public string? MasqueradeStringContent { get; set; }
    public string? MasqueradeStringHeaders { get; set; }
    public int? MasqueradeStringStatusCode { get; set; }

    // DNS 解析器
    public string? ResolverType { get; set; }
    public string? ResolverTcpAddr { get; set; }
    public string? ResolverUdpAddr { get; set; }
    public string? ResolverTlsAddr { get; set; }

    // 运营管理
    public decimal? ServerCost { get; set; }
    public string? BillingCycle { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DomainName { get; set; }
    public string? Remark { get; set; }
}
