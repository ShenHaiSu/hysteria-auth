using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class Node
{
    // ===== 基础身份（原有字段，保持不变） =====
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; }

    [Required]
    [MaxLength(256)]
    public string SecretKey { get; set; } = string.Empty;

    public int SecretVersion { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastHeartbeat { get; set; }

    [MaxLength(128)]
    public string? Location { get; set; }

    public int? TrafficStatsPort { get; set; }

    [MaxLength(256)]
    public string? TrafficStatsSecret { get; set; }

    [MaxLength(128)]
    public string? ProvisionToken { get; set; }

    [Required]
    [MaxLength(16)]
    public string ProvisionStatus { get; set; } = "pending";

    // ===== 监听 & 端口跳跃（新字段，映射 listen） =====
    [MaxLength(45)]
    public string? ListenAddress { get; set; } = "0.0.0.0";

    public int? ListenPort { get; set; } = 6789;

    public bool EnablePortHopping { get; set; } = true;

    public int? PortHopRangeStart { get; set; } = 61000;

    public int? PortHopRangeEnd { get; set; } = 63000;

    // ===== 混淆（新字段，映射 obfs） =====
    [MaxLength(32)]
    public string? ObfsType { get; set; } = "salamander";

    [MaxLength(256)]
    public string? ObfsPassword { get; set; }

    // ===== QUIC & 拥塞控制（新字段，映射 quic / congestion） =====
    [MaxLength(16)]
    public string? CongestionControl { get; set; } = "bbr";

    public long? BrutalTxBandwidth { get; set; }

    public int? QuicMaxIdleTimeout { get; set; } = 30;

    public int? QuicMaxUdpPayloadSize { get; set; } = 1350;

    public long? QuicInitStreamReceiveWindow { get; set; }

    public long? QuicMaxStreamReceiveWindow { get; set; }

    public long? QuicInitConnectionReceiveWindow { get; set; }

    public long? QuicMaxConnectionReceiveWindow { get; set; }

    // ===== 带宽（新字段，映射 bandwidth） =====
    [MaxLength(16)]
    public string? BandwidthUp { get; set; }

    [MaxLength(16)]
    public string? BandwidthDown { get; set; }

    public bool? IgnoreClientBandwidth { get; set; } = false;

    // ===== 速度测试（新字段，映射 speedTest） =====
    public bool? EnableSpeedTest { get; set; } = false;

    public int? SpeedTestPingInterval { get; set; } = 60;

    public long? SpeedTestDownloadSize { get; set; }

    public long? SpeedTestUploadSize { get; set; }

    // ===== UDP（新字段，映射 udpIdleTimeout） =====
    public int? UdpIdleTimeout { get; set; } = 60;

    // ===== 协议嗅探（新字段，映射 sniff） =====
    public bool? SniffEnabled { get; set; } = false;

    public int? SniffTimeout { get; set; } = 5;

    public bool? SniffRespectHttps { get; set; } = false;

    // ===== 伪装（新字段，映射 masquerade） =====
    [MaxLength(16)]
    public string? MasqueradeType { get; set; }

    [MaxLength(512)]
    public string? MasqueradeFile { get; set; }

    [MaxLength(512)]
    public string? MasqueradeProxyUrl { get; set; }

    public string? MasqueradeStringContent { get; set; }

    public string? MasqueradeStringHeaders { get; set; }

    public int? MasqueradeStringStatusCode { get; set; } = 200;

    public int? MasqueradeReplyBps { get; set; }

    // ===== DNS 解析器（新字段，映射 resolver） =====
    [MaxLength(16)]
    public string? ResolverType { get; set; } = "system";

    [MaxLength(64)]
    public string? ResolverTcpAddr { get; set; }

    [MaxLength(64)]
    public string? ResolverUdpAddr { get; set; }

    [MaxLength(64)]
    public string? ResolverTlsAddr { get; set; }

    public int? ResolverResolveInterval { get; set; }

    public int? ResolverResolveConcurrency { get; set; }

    // ===== 配置版本追踪（新字段） =====
    public int ConfigVersion { get; set; } = 1;

    public DateTime? ConfigUpdatedAt { get; set; }

    // ===== 运营管理（新字段） =====
    [Column(TypeName = "decimal(10,2)")]
    public decimal? ServerCost { get; set; }

    [MaxLength(16)]
    public string? BillingCycle { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [MaxLength(256)]
    public string? DomainName { get; set; }

    public string? Remark { get; set; }
}
