# Phase 7: 节点数据表扩展 — 完整 Hysteria 2 配置管理

> **阶段**: Phase 7 | **依赖**: Phase 6（SPA 集成部署）+ 审计模块
>
> **来源文档**: [`document/architect/node-table-redesign.md`](../architect/node-table-redesign.md) · [`document/hysteria/hysteria-server-config.md`](../hysteria/hysteria-server-config.md) · [`document/architect/database-design.md`](../architect/database-design.md) §2.5 · [`document/architect/edge-node-design.md`](../architect/edge-node-design.md) · [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md)

---

## 1. 阶段目标与范围

### 1.1 总体目标

将 [`Node`](../../src/HysteriaAuth.Master/Models/Entities/Node.cs) 实体从 14 个字段扩展至 ~60 个字段，覆盖 Hysteria 2 服务端的全部配置项（监听、端口跳跃、混淆、QUIC、拥塞控制、带宽、速度测试、UDP、协议嗅探、伪装、DNS 解析器），使 Master 能够作为 Hysteria 2 服务端配置的唯一权威来源，通过配置同步 API 将完整 YAML 配置下发到 Edge Agent，实现从控制面板到边缘节点的全链路配置管理。

同时增加 5 个运营管理字段（费用、续费周期、到期日期、域名、备注）便于运维人员管理节点生命周期。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 7.1 | Node Entity 扩展 | [`Node.cs`](../../src/HysteriaAuth.Master/Models/Entities/Node.cs) 新增 ~46 个配置/运营字段 |
| 7.2 | EF Core Migration | 生成并应用数据库迁移（仅 ADD COLUMN，向后兼容） |
| 7.3 | DTO 扩展 | 新增 `UpdateNodeConfigRequest`，扩展 `NodeDto`、`PreRegisterNodeRequest`、`NodeConfigResponse` 等 |
| 7.4 | `ConfigGeneratorService` | 新服务：将 Node Entity 映射为 Hysteria 2 YAML 字符串 |
| 7.5 | `NodeService` 扩展 | 预注册/注册时初始化新字段；新增 `UpdateNodeConfigAsync` 方法；配置同步返回完整 YAML |
| 7.6 | `NodesController` 扩展 | 新增 `PUT /api/v1/admin/nodes/{nodeId}/config` 端点 |
| 7.7 | `AesEncryptionService` 扩展 | 增加 `ObfsPassword` 加密/解密支持 |
| 7.8 | Edge Agent 适配 | 解析新增的 `configYaml` 字段，应用 iptables 端口跳跃规则 |
| 7.9 | DI 注册 | `Program.cs` 注册 `ConfigGeneratorService` |
| 7.10 | 审计日志扩展 | 配置变更操作写入审计日志 |

---

## 2. 阶段启动前置检查

> Phase 7 启动前，必须验证以下前置条件。

| # | 检查项 | 验证内容 | 通过标准 |
|---|--------|----------|----------|
| P7.1 | `Node` Entity 当前仅 14 字段 | 检查 [`Node.cs`](../../src/HysteriaAuth.Master/Models/Entities/Node.cs) 行数 | 约 49 行，14 个属性 |
| P7.2 | `AppDbContext` 中 Nodes 配置完整 | 检查 `OnModelCreating` Node 部分 | `ProvisionToken` 唯一索引已配置 |
| P7.3 | `NodeService.PreRegisterNodeAsync` 已实现 | 检查 [`NodeService.cs`](../../src/HysteriaAuth.Master/Services/NodeService.cs:56) | 方法存在，接受 `PreRegisterNodeRequest` |
| P7.4 | `NodeService.RegisterWithTokenAsync` 已实现 | 检查 [`NodeService.cs`](../../src/HysteriaAuth.Master/Services/NodeService.cs:107) | 方法存在，返回 `RegisterWithTokenResponse` |
| P7.5 | `NodeService.GetNodeConfigAsync` 已实现 | 检查 [`NodeService.cs`](../../src/HysteriaAuth.Master/Services/NodeService.cs:205) | 返回 `NodeConfigResponse`（含 `Config` 字段） |
| P7.6 | `AesEncryptionService` 就绪 | 检查 `Encrypt`/`Decrypt` 方法 | 使用 AES-256-GCM |
| P7.7 | `AuditService` 就绪 | 检查 `LogAsync` 方法 | 支持 action/targetType/targetId/detail/clientIp |
| P7.8 | `NodeDto` 当前仅基础字段 | 检查 [`NodeDto.cs`](../../src/HysteriaAuth.Master/Models/DTOs/NodeDto.cs:120) | 约 10 个属性 |
| P7.9 | `MapToDto` 静态方法存在 | 检查 [`NodeService.cs`](../../src/HysteriaAuth.Master/Services/NodeService.cs:520) | 位于 `NodeService` 底部 |
| P7.10 | ✅ 阅读 [`node-table-redesign.md`](../architect/node-table-redesign.md) 全部内容 | — | — |
| P7.11 | ✅ 阅读 [`hysteria-server-config.md`](../hysteria/hysteria-server-config.md) §1–§15 | — | — |

---

## 3. 具体任务清单

### 第 1 天：Entity + Migration + DTO

#### 3.1 Node Entity 扩展

- [ ] **7.1.1** 修改 [`Node.cs`](../../src/HysteriaAuth.Master/Models/Entities/Node.cs)，在原有 14 个字段后按分组追加新字段：

```csharp
// ===== 监听 & 端口跳跃（新字段） =====
[MaxLength(45)]
public string? ListenAddress { get; set; } = "0.0.0.0";

public int? ListenPort { get; set; } = 6789;

public bool EnablePortHopping { get; set; } = true;

public int? PortHopRangeStart { get; set; } = 61000;

public int? PortHopRangeEnd { get; set; } = 63000;

// ===== 混淆（新字段） =====
[MaxLength(32)]
public string? ObfsType { get; set; } = "salamander";

[MaxLength(256)]
public string? ObfsPassword { get; set; }

// ===== QUIC & 拥塞控制（新字段） =====
[MaxLength(16)]
public string? CongestionControl { get; set; } = "bbr";

public long? BrutalTxBandwidth { get; set; }

public int? QuicMaxIdleTimeout { get; set; } = 30;

public int? QuicMaxUdpPayloadSize { get; set; } = 1350;

public long? QuicInitStreamReceiveWindow { get; set; }

public long? QuicMaxStreamReceiveWindow { get; set; }

public long? QuicInitConnectionReceiveWindow { get; set; }

public long? QuicMaxConnectionReceiveWindow { get; set; }

// ===== 带宽（新字段） =====
[MaxLength(16)]
public string? BandwidthUp { get; set; }

[MaxLength(16)]
public string? BandwidthDown { get; set; }

public bool? IgnoreClientBandwidth { get; set; } = false;

// ===== 速度测试（新字段） =====
public bool? EnableSpeedTest { get; set; } = false;

public int? SpeedTestPingInterval { get; set; } = 60;

public long? SpeedTestDownloadSize { get; set; }

public long? SpeedTestUploadSize { get; set; }

// ===== UDP（新字段） =====
public int? UdpIdleTimeout { get; set; } = 60;

// ===== 协议嗅探（新字段） =====
public bool? SniffEnabled { get; set; } = false;

public int? SniffTimeout { get; set; } = 5;

public bool? SniffRespectHttps { get; set; } = false;

// ===== 伪装（新字段） =====
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

// ===== DNS 解析器（新字段） =====
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
```

> **重要**：留空的字段（无默认值的不填）如 `ObfsPassword`、`BandwidthUp`、`BandwidthDown` 等在 YAML 生成时以 NULL 判定是否省略该配置段。

#### 3.2 EF Core Migration

- [ ] **7.2.1** 在项目根目录执行 Migration 创建命令：

```bash
cd src/HysteriaAuth.Master && dotnet ef migrations add ExpandNodeTable --output-dir Migrations
```

- [ ] **7.2.2** 验证 Migration 文件内容：
  - 确认 `Up` 方法中仅包含 `AddColumn` 调用，无 `DropColumn` 或表结构变更
  - 确认所有新列的 `nullable: true`（对于可为 NULL 的字段）
  - 确认 `EnablePortHopping` 的 `defaultValue: true`
  - 确认 `ConfigVersion` 的 `defaultValue: 1`

- [ ] **7.2.3** 应用 Migration：

```bash
cd src/HysteriaAuth.Master && dotnet ef database update
```

- [ ] **7.2.4** 验证迁移后的数据库：
  - 使用 SQLite 工具查询 `Nodes` 表结构，确认所有新列已添加
  - 查询一条现有节点记录，确认原有数据未受影响
  - 确认 `Port` 列仍存在（向后兼容）

```sql
-- 验证表结构
PRAGMA table_info(Nodes);

-- 验证现有数据
SELECT Id, Name, IpAddress, Port, ListenPort, EnablePortHopping, ConfigVersion FROM Nodes LIMIT 5;
```

#### 3.3 DTO 扩展

- [ ] **7.3.1** 修改 [`NodeDto.cs`](../../src/HysteriaAuth.Master/Models/DTOs/NodeDto.cs)：

  - 扩展 `NodeDto` 类，追加所有新增字段（用于管理员面板展示）：
```csharp
// 监听 & 端口跳跃
public string? ListenAddress { get; set; }
public int? ListenPort { get; set; }
public bool EnablePortHopping { get; set; }
public int? PortHopRangeStart { get; set; }
public int? PortHopRangeEnd { get; set; }
// 混淆
public string? ObfsType { get; set; }
// 拥塞控制
public string? CongestionControl { get; set; }
public long? BrutalTxBandwidth { get; set; }
// 带宽
public string? BandwidthUp { get; set; }
public string? BandwidthDown { get; set; }
public bool? IgnoreClientBandwidth { get; set; }
// 速度测试
public bool? EnableSpeedTest { get; set; }
// UDP
public int? UdpIdleTimeout { get; set; }
// 协议嗅探
public bool? SniffEnabled { get; set; }
// 伪装
public string? MasqueradeType { get; set; }
public string? MasqueradeFile { get; set; }
// DNS 解析器
public string? ResolverType { get; set; }
// 配置版本
public int ConfigVersion { get; set; }
public DateTime? ConfigUpdatedAt { get; set; }
// 运营管理
public decimal? ServerCost { get; set; }
public string? BillingCycle { get; set; }
public DateTime? ExpirationDate { get; set; }
public string? DomainName { get; set; }
public string? Remark { get; set; }
```

  - 扩展 `NodeConfigResponse`，增加 `ConfigVersion` 和 `ConfigYaml` 字段：
```csharp
public class NodeConfigResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ConfigVersion { get; set; }
    public string? ConfigYaml { get; set; }   // 新增：完整 Hysteria 2 YAML 配置
    public NodeConfigInfo Config { get; set; } = new();  // 保留：Edge Agent 基本配置
}
```

  - 扩展 `PreRegisterNodeRequest`，增加可选的 `listenPort`、`domainName`、`remark` 字段：
```csharp
public class PreRegisterNodeRequest
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Location { get; set; }
    public int? TrafficStatsPort { get; set; }
    // 新增可选字段
    public int? ListenPort { get; set; }
    public string? DomainName { get; set; }
    public string? Remark { get; set; }
}
```

- [ ] **7.3.2** 新建 `Models/DTOs/UpdateNodeConfigRequest.cs`：

```csharp
namespace HysteriaAuth.Master.Models.DTOs;

/// <summary>
/// 管理员更新节点配置的请求 DTO。所有字段均为可选（null 表示不修改）。
/// </summary>
public class UpdateNodeConfigRequest
{
    // 监听
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
```

---

### 第 2 天：ConfigGeneratorService + NodeService 扩展

#### 3.4 ConfigGeneratorService

- [ ] **7.4.1** 新建 [`Services/ConfigGeneratorService.cs`](../../src/HysteriaAuth.Master/Services/ConfigGeneratorService.cs)：

```csharp
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// Hysteria 2 服务端 YAML 配置生成器。
/// 将 Node Entity 的配置字段映射为完整的 Hysteria 2 YAML 格式。
/// </summary>
public class ConfigGeneratorService
{
    private readonly AesEncryptionService _aes;
    private readonly IConfiguration _configuration;

    public ConfigGeneratorService(AesEncryptionService aes, IConfiguration configuration)
    {
        _aes = aes;
        _configuration = configuration;
    }

    /// <summary>
    /// 根据 Node Entity 生成完整的 Hysteria 2 服务端 YAML 配置字符串。
    /// </summary>
    public string GenerateYaml(Node node)
    {
        var masterHost = _configuration.GetValue<string>("MasterServerUrl") ?? "https://master.example.com";
        var sb = new System.Text.StringBuilder();

        // 文件头注释
        sb.AppendLine($"# 自动生成于 {DateTime.UtcNow:O} | 节点: {node.Name} | 配置版本: {node.ConfigVersion}");
        sb.AppendLine();

        // 1. listen（端口跳跃逻辑）
        GenerateListen(sb, node);

        // 2. realm（域名）
        if (!string.IsNullOrWhiteSpace(node.DomainName))
        {
            sb.AppendLine($"realm: {node.DomainName}");
        }

        // 3. obfs
        GenerateObfs(sb, node);

        // 4. quic
        GenerateQuic(sb, node);

        // 5. bandwidth
        GenerateBandwidth(sb, node);

        // 6. speedTest
        GenerateSpeedTest(sb, node);

        // 7. udpIdleTimeout
        GenerateUdp(sb, node);

        // 8. auth（HTTP 认证指向 Master 的 AuthController）
        GenerateAuth(sb, node, masterHost);

        // 9. resolver
        GenerateResolver(sb, node);

        // 10. sniff
        GenerateSniff(sb, node);

        // 11. masquerade
        GenerateMasquerade(sb, node);

        // 12. trafficStats
        GenerateTrafficStats(sb, node);

        return sb.ToString();
    }

    private void GenerateListen(System.Text.StringBuilder sb, Node node)
    {
        var addr = node.ListenAddress ?? "0.0.0.0";
        var port = node.ListenPort ?? node.Port;

        if (node.EnablePortHopping && node.PortHopRangeStart.HasValue && node.PortHopRangeEnd.HasValue)
        {
            sb.AppendLine($"listen: {addr}:{port},{addr}:{node.PortHopRangeStart}-{node.PortHopRangeEnd}");
        }
        else
        {
            sb.AppendLine($"listen: {addr}:{port}");
        }
        sb.AppendLine();
    }

    private void GenerateObfs(System.Text.StringBuilder sb, Node node)
    {
        if (string.IsNullOrWhiteSpace(node.ObfsType) || string.IsNullOrWhiteSpace(node.ObfsPassword))
            return;

        var password = _aes.Decrypt(node.ObfsPassword);
        sb.AppendLine("obfs:");
        sb.AppendLine($"  type: {node.ObfsType}");
        sb.AppendLine($"  {node.ObfsType}:");
        sb.AppendLine($"    password: \"{password}\"");
        sb.AppendLine();
    }

    private void GenerateQuic(System.Text.StringBuilder sb, Node node)
    {
        bool hasQuicConfig = node.QuicMaxIdleTimeout.HasValue
            || node.QuicMaxUdpPayloadSize.HasValue
            || node.QuicInitStreamReceiveWindow.HasValue
            || node.QuicMaxStreamReceiveWindow.HasValue
            || node.QuicInitConnectionReceiveWindow.HasValue
            || node.QuicMaxConnectionReceiveWindow.HasValue;

        if (!hasQuicConfig) return;

        sb.AppendLine("quic:");
        if (node.QuicMaxIdleTimeout.HasValue)
            sb.AppendLine($"  maxIdleTimeout: {node.QuicMaxIdleTimeout}s");
        if (node.QuicMaxUdpPayloadSize.HasValue && node.QuicMaxUdpPayloadSize != 1350)
            sb.AppendLine($"  maxUDPPayloadSize: {node.QuicMaxUdpPayloadSize}");

        // 仅在非默认值时输出流/连接窗口
        var windows = new List<string>();
        if (node.QuicInitStreamReceiveWindow.HasValue)
            windows.Add($"initStreamReceiveWindow: {node.QuicInitStreamReceiveWindow}");
        if (node.QuicMaxStreamReceiveWindow.HasValue)
            windows.Add($"maxStreamReceiveWindow: {node.QuicMaxStreamReceiveWindow}");
        if (node.QuicInitConnectionReceiveWindow.HasValue)
            windows.Add($"initConnectionReceiveWindow: {node.QuicInitConnectionReceiveWindow}");
        if (node.QuicMaxConnectionReceiveWindow.HasValue)
            windows.Add($"maxConnectionReceiveWindow: {node.QuicMaxConnectionReceiveWindow}");

        if (windows.Count > 0)
        {
            sb.AppendLine($"  initialStreamReceiveWindow: {node.QuicInitStreamReceiveWindow ?? 8388608}");
            sb.AppendLine($"  maxStreamReceiveWindow: {node.QuicMaxStreamReceiveWindow ?? 8388608}");
        }
        sb.AppendLine();
    }

    private void GenerateBandwidth(System.Text.StringBuilder sb, Node node)
    {
        bool hasBandwidth = !string.IsNullOrWhiteSpace(node.BandwidthUp)
            || !string.IsNullOrWhiteSpace(node.BandwidthDown)
            || !string.IsNullOrWhiteSpace(node.CongestionControl);

        if (!hasBandwidth) return;

        sb.AppendLine("bandwidth:");
        if (!string.IsNullOrWhiteSpace(node.BandwidthUp))
            sb.AppendLine($"  up: {node.BandwidthUp}");
        if (!string.IsNullOrWhiteSpace(node.BandwidthDown))
            sb.AppendLine($"  down: {node.BandwidthDown}");

        // 拥塞控制
        if (!string.IsNullOrWhiteSpace(node.CongestionControl))
        {
            sb.AppendLine("  congestion:");
            sb.AppendLine($"    algorithm: {node.CongestionControl}");
            if (node.CongestionControl == "brutal" && node.BrutalTxBandwidth.HasValue)
                sb.AppendLine($"    brutal:\n      txBandwidth: {node.BrutalTxBandwidth}");
        }

        if (node.IgnoreClientBandwidth == true)
            sb.AppendLine("  ignoreClientBandwidth: true");

        sb.AppendLine();
    }

    private void GenerateSpeedTest(System.Text.StringBuilder sb, Node node)
    {
        if (node.EnableSpeedTest != true) return;

        sb.AppendLine("speedTest:");
        if (node.SpeedTestPingInterval.HasValue)
            sb.AppendLine($"  pingInterval: {node.SpeedTestPingInterval}s");
        if (node.SpeedTestDownloadSize.HasValue)
            sb.AppendLine($"  downloadSize: {node.SpeedTestDownloadSize}");
        if (node.SpeedTestUploadSize.HasValue)
            sb.AppendLine($"  uploadSize: {node.SpeedTestUploadSize}");
        sb.AppendLine();
    }

    private void GenerateUdp(System.Text.StringBuilder sb, Node node)
    {
        if (node.UdpIdleTimeout.HasValue)
        {
            sb.AppendLine($"udpIdleTimeout: {node.UdpIdleTimeout}s");
            sb.AppendLine();
        }
    }

    private void GenerateAuth(System.Text.StringBuilder sb, Node node, string masterHost)
    {
        sb.AppendLine("auth:");
        sb.AppendLine("  type: http");
        sb.AppendLine("  http:");
        sb.AppendLine($"    url: {masterHost.TrimEnd('/')}/api/v1/nodes/{node.Id}/auth");
        sb.AppendLine($"    secret: \"{_aes.Decrypt(node.SecretKey)}\"");
        sb.AppendLine();
    }

    private void GenerateResolver(System.Text.StringBuilder sb, Node node)
    {
        if (string.IsNullOrWhiteSpace(node.ResolverType) || node.ResolverType == "system")
            return;

        sb.AppendLine("resolver:");
        sb.AppendLine($"  type: {node.ResolverType}");

        switch (node.ResolverType)
        {
            case "tcp" when !string.IsNullOrWhiteSpace(node.ResolverTcpAddr):
                sb.AppendLine("  tcp:");
                sb.AppendLine($"    addr: {node.ResolverTcpAddr}");
                break;
            case "udp" when !string.IsNullOrWhiteSpace(node.ResolverUdpAddr):
                sb.AppendLine("  udp:");
                sb.AppendLine($"    addr: {node.ResolverUdpAddr}");
                break;
            case "tls" when !string.IsNullOrWhiteSpace(node.ResolverTlsAddr):
                sb.AppendLine("  tls:");
                sb.AppendLine($"    addr: {node.ResolverTlsAddr}");
                break;
        }
        sb.AppendLine();
    }

    private void GenerateSniff(System.Text.StringBuilder sb, Node node)
    {
        if (node.SniffEnabled != true) return;

        sb.AppendLine("sniff:");
        if (node.SniffTimeout.HasValue)
            sb.AppendLine($"  timeout: {node.SniffTimeout}s");
        if (node.SniffRespectHttps == true)
            sb.AppendLine("  respectHTTPS: true");
        sb.AppendLine();
    }

    private void GenerateMasquerade(System.Text.StringBuilder sb, Node node)
    {
        if (string.IsNullOrWhiteSpace(node.MasqueradeType)) return;

        sb.AppendLine("masquerade:");
        sb.AppendLine($"  type: {node.MasqueradeType}");

        switch (node.MasqueradeType)
        {
            case "file" when !string.IsNullOrWhiteSpace(node.MasqueradeFile):
                sb.AppendLine("  file:");
                sb.AppendLine($"    dir: {node.MasqueradeFile}");
                break;
            case "proxy" when !string.IsNullOrWhiteSpace(node.MasqueradeProxyUrl):
                sb.AppendLine("  proxy:");
                sb.AppendLine($"    url: {node.MasqueradeProxyUrl}");
                break;
            case "string" when !string.IsNullOrWhiteSpace(node.MasqueradeStringContent):
                sb.AppendLine("  string:");
                sb.AppendLine($"    content: \"{node.MasqueradeStringContent}\"");
                if (!string.IsNullOrWhiteSpace(node.MasqueradeStringHeaders))
                    sb.AppendLine($"    headers: {node.MasqueradeStringHeaders}");
                if (node.MasqueradeStringStatusCode.HasValue)
                    sb.AppendLine($"    statusCode: {node.MasqueradeStringStatusCode}");
                break;
        }
        sb.AppendLine();
    }

    private void GenerateTrafficStats(System.Text.StringBuilder sb, Node node)
    {
        if (!node.TrafficStatsPort.HasValue) return;

        sb.AppendLine("trafficStats:");
        sb.AppendLine($"  listen: 127.0.0.1:{node.TrafficStatsPort}");
        if (!string.IsNullOrWhiteSpace(node.TrafficStatsSecret))
            sb.AppendLine($"  secret: \"{_aes.Decrypt(node.TrafficStatsSecret)}\"");
        sb.AppendLine();
    }
}
```

#### 3.5 AesEncryptionService 扩展

- [ ] **7.5.1** 修改 [`AesEncryptionService.cs`](../../src/HysteriaAuth.Master/Services/AesEncryptionService.cs)：

> 当前 `AesEncryptionService` 已支持 `SecretKey` 和 `TrafficStatsSecret` 的加密/解密。需确认 `ObfsPassword` 字段在写入和读取时调用相同的 `Encrypt`/`Decrypt` 方法，无需新增方法。

- 验证点：`NodeService` 中所有对 `ObfsPassword` 的写入操作都通过 `_aes.Encrypt()` 包装，所有读取操作都通过 `_aes.Decrypt()` 解包。

#### 3.6 NodeService 扩展

- [ ] **7.6.1** 修改 `PreRegisterNodeAsync` 方法（[`NodeService.cs:56`](../../src/HysteriaAuth.Master/Services/NodeService.cs:56)），创建 Node 时初始化新字段：

```csharp
var node = new Node
{
    Id = nodeId,
    Name = request.Name,
    Port = request.Port,
    Location = request.Location,
    TrafficStatsPort = request.TrafficStatsPort,
    ProvisionToken = provisionToken,
    ProvisionStatus = "pending",
    IsActive = false,
    CreatedAt = DateTime.UtcNow,
    // 新字段初始化
    ListenPort = request.ListenPort ?? 6789,
    DomainName = request.DomainName,
    Remark = request.Remark
};
```

- [ ] **7.6.2** 修改 `GetNodeConfigAsync` 方法（[`NodeService.cs:205`](../../src/HysteriaAuth.Master/Services/NodeService.cs:205)），注入 `ConfigGeneratorService` 并返回完整 YAML：

```csharp
public async Task<NodeConfigResponse> GetNodeConfigAsync(string nodeId)
{
    var node = await _nodeRepo.GetByIdAsync(nodeId);
    if (node == null)
        throw new NotFoundException("节点不存在");

    var configYaml = _configGenerator.GenerateYaml(node);

    return new NodeConfigResponse
    {
        NodeId = node.Id,
        NodeSecret = _aes.Decrypt(node.SecretKey),
        IsActive = node.IsActive,
        ConfigVersion = node.ConfigVersion,
        ConfigYaml = configYaml,
        Config = new NodeConfigInfo
        {
            AuthProxyPort = 8080,
            HealthCheckPort = 8081,
            TrafficStatsPort = node.TrafficStatsPort ?? 9999,
            CollectIntervalSeconds = 30,
            HeartbeatIntervalSeconds = 30
        }
    };
}
```

- [ ] **7.6.3** 构造函数注入 `ConfigGeneratorService`：

```csharp
private readonly ConfigGeneratorService _configGenerator;

public NodeService(
    // ... 现有参数 ...
    ConfigGeneratorService configGenerator)
{
    // ... 现有赋值 ...
    _configGenerator = configGenerator;
}
```

- [ ] **7.6.4** 新增 `UpdateNodeConfigAsync` 方法 — 管理员更新节点配置：

```csharp
/// <summary>
/// 管理员更新节点配置。仅更新请求中提供的非 null 字段。
/// 更新后递增 ConfigVersion，写入审计日志。
/// </summary>
public async Task<NodeDto> UpdateNodeConfigAsync(
    string nodeId, UpdateNodeConfigRequest request, long adminId, string clientIp)
{
    var node = await _nodeRepo.GetByIdAsync(nodeId);
    if (node == null)
        throw new NotFoundException("节点不存在");

    // 逐一判断非 null 才赋值
    if (request.ListenAddress != null) node.ListenAddress = request.ListenAddress;
    if (request.ListenPort.HasValue) node.ListenPort = request.ListenPort;
    if (request.EnablePortHopping.HasValue) node.EnablePortHopping = request.EnablePortHopping.Value;
    if (request.PortHopRangeStart.HasValue) node.PortHopRangeStart = request.PortHopRangeStart;
    if (request.PortHopRangeEnd.HasValue) node.PortHopRangeEnd = request.PortHopRangeEnd;
    if (request.ObfsType != null) node.ObfsType = request.ObfsType;
    if (request.ObfsPassword != null) node.ObfsPassword = _aes.Encrypt(request.ObfsPassword);
    if (request.CongestionControl != null) node.CongestionControl = request.CongestionControl;
    if (request.BrutalTxBandwidth.HasValue) node.BrutalTxBandwidth = request.BrutalTxBandwidth;
    if (request.QuicMaxIdleTimeout.HasValue) node.QuicMaxIdleTimeout = request.QuicMaxIdleTimeout;
    if (request.QuicMaxUdpPayloadSize.HasValue) node.QuicMaxUdpPayloadSize = request.QuicMaxUdpPayloadSize;
    if (request.BandwidthUp != null) node.BandwidthUp = request.BandwidthUp;
    if (request.BandwidthDown != null) node.BandwidthDown = request.BandwidthDown;
    if (request.IgnoreClientBandwidth.HasValue) node.IgnoreClientBandwidth = request.IgnoreClientBandwidth;
    if (request.EnableSpeedTest.HasValue) node.EnableSpeedTest = request.EnableSpeedTest;
    if (request.SpeedTestPingInterval.HasValue) node.SpeedTestPingInterval = request.SpeedTestPingInterval;
    if (request.UdpIdleTimeout.HasValue) node.UdpIdleTimeout = request.UdpIdleTimeout;
    if (request.SniffEnabled.HasValue) node.SniffEnabled = request.SniffEnabled;
    if (request.SniffTimeout.HasValue) node.SniffTimeout = request.SniffTimeout;
    if (request.SniffRespectHttps.HasValue) node.SniffRespectHttps = request.SniffRespectHttps;
    if (request.MasqueradeType != null) node.MasqueradeType = request.MasqueradeType;
    if (request.MasqueradeFile != null) node.MasqueradeFile = request.MasqueradeFile;
    if (request.MasqueradeProxyUrl != null) node.MasqueradeProxyUrl = request.MasqueradeProxyUrl;
    if (request.MasqueradeStringContent != null) node.MasqueradeStringContent = request.MasqueradeStringContent;
    if (request.MasqueradeStringHeaders != null) node.MasqueradeStringHeaders = request.MasqueradeStringHeaders;
    if (request.MasqueradeStringStatusCode.HasValue) node.MasqueradeStringStatusCode = request.MasqueradeStringStatusCode;
    if (request.ResolverType != null) node.ResolverType = request.ResolverType;
    if (request.ResolverTcpAddr != null) node.ResolverTcpAddr = request.ResolverTcpAddr;
    if (request.ResolverUdpAddr != null) node.ResolverUdpAddr = request.ResolverUdpAddr;
    if (request.ResolverTlsAddr != null) node.ResolverTlsAddr = request.ResolverTlsAddr;
    if (request.ServerCost.HasValue) node.ServerCost = request.ServerCost;
    if (request.BillingCycle != null) node.BillingCycle = request.BillingCycle;
    if (request.ExpirationDate.HasValue) node.ExpirationDate = request.ExpirationDate;
    if (request.DomainName != null) node.DomainName = request.DomainName;
    if (request.Remark != null) node.Remark = request.Remark;

    // 递增配置版本
    node.ConfigVersion++;
    node.ConfigUpdatedAt = DateTime.UtcNow;

    await _nodeRepo.UpdateAsync(node);

    _logger.LogInformation("管理员更新节点配置: {NodeId}, 配置版本: {Version}", nodeId, node.ConfigVersion);

    // 写入审计日志
    await _auditService.LogAsync(
        adminId: adminId,
        action: "update_config",
        targetType: "node",
        targetId: nodeId,
        detail: new { configVersion = node.ConfigVersion, updatedFields = GetUpdatedFieldNames(request) },
        clientIp: clientIp
    );

    return MapToDto(node);
}

/// <summary>
/// 提取请求中非 null 的字段名列表（用于审计日志）。
/// </summary>
private static List<string> GetUpdatedFieldNames(UpdateNodeConfigRequest request)
{
    var fields = new List<string>();
    if (request.ListenAddress != null) fields.Add(nameof(request.ListenAddress));
    if (request.ListenPort.HasValue) fields.Add(nameof(request.ListenPort));
    if (request.EnablePortHopping.HasValue) fields.Add(nameof(request.EnablePortHopping));
    if (request.ObfsType != null) fields.Add(nameof(request.ObfsType));
    if (request.ObfsPassword != null) fields.Add(nameof(request.ObfsPassword));
    if (request.CongestionControl != null) fields.Add(nameof(request.CongestionControl));
    if (request.BandwidthUp != null) fields.Add(nameof(request.BandwidthUp));
    if (request.BandwidthDown != null) fields.Add(nameof(request.BandwidthDown));
    if (request.DomainName != null) fields.Add(nameof(request.DomainName));
    if (request.Remark != null) fields.Add(nameof(request.Remark));
    return fields;
}
```

- [ ] **7.6.5** 修改 `MapToDto` 方法（[`NodeService.cs:520`](../../src/HysteriaAuth.Master/Services/NodeService.cs:520)），追加所有新字段映射：

```csharp
private static NodeDto MapToDto(Node node)
{
    return new NodeDto
    {
        Id = node.Id,
        Name = node.Name,
        IpAddress = node.IpAddress,
        Port = node.Port,
        IsActive = node.IsActive,
        CreatedAt = node.CreatedAt,
        LastHeartbeat = node.LastHeartbeat,
        Location = node.Location,
        TrafficStatsPort = node.TrafficStatsPort,
        ProvisionStatus = node.ProvisionStatus,
        // 新字段
        ListenAddress = node.ListenAddress,
        ListenPort = node.ListenPort,
        EnablePortHopping = node.EnablePortHopping,
        PortHopRangeStart = node.PortHopRangeStart,
        PortHopRangeEnd = node.PortHopRangeEnd,
        ObfsType = node.ObfsType,
        CongestionControl = node.CongestionControl,
        BrutalTxBandwidth = node.BrutalTxBandwidth,
        BandwidthUp = node.BandwidthUp,
        BandwidthDown = node.BandwidthDown,
        IgnoreClientBandwidth = node.IgnoreClientBandwidth,
        EnableSpeedTest = node.EnableSpeedTest,
        UdpIdleTimeout = node.UdpIdleTimeout,
        SniffEnabled = node.SniffEnabled,
        MasqueradeType = node.MasqueradeType,
        MasqueradeFile = node.MasqueradeFile,
        ResolverType = node.ResolverType,
        ConfigVersion = node.ConfigVersion,
        ConfigUpdatedAt = node.ConfigUpdatedAt,
        ServerCost = node.ServerCost,
        BillingCycle = node.BillingCycle,
        ExpirationDate = node.ExpirationDate,
        DomainName = node.DomainName,
        Remark = node.Remark
    };
}
```

---

### 第 3 天：Controller + DI 注册 + Edge Agent 适配

#### 3.7 NodesController 扩展

- [ ] **7.7.1** 在 [`NodesController.cs`](../../src/HysteriaAuth.Master/Controllers/NodesController.cs) 中新增 `UpdateNodeConfig` 端点：

```csharp
/// <summary>
/// 管理员更新节点配置（含 Hysteria 2 完整配置项和运营字段）。
/// 更新后配置版本号自动递增，Edge Agent 下次心跳/配置同步时拉取新配置。
/// </summary>
[HttpPut("admin/nodes/{nodeId}/config")]
public async Task<IActionResult> UpdateNodeConfig(
    string nodeId,
    [FromBody] UpdateNodeConfigRequest request)
{
    var adminId = (long)HttpContext.Items["AdminId"]!;
    var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
    var result = await _nodeService.UpdateNodeConfigAsync(nodeId, request, adminId, clientIp);
    return Ok(result);
}
```

#### 3.8 DI 注册

- [ ] **7.8.1** 修改 [`Program.cs`](../../src/HysteriaAuth.Master/Program.cs)，注册 `ConfigGeneratorService`：

```csharp
// 在 builder.Services 区域添加：
builder.Services.AddSingleton<ConfigGeneratorService>();
```

> 注意：`ConfigGeneratorService` 不依赖数据库上下文，属于无状态服务，使用 `Singleton` 生命周期即可。但因为它依赖 `AesEncryptionService`（可能是 Scoped），如果编译报错则改为 `Scoped`：
> ```csharp
> builder.Services.AddScoped<ConfigGeneratorService>();
> ```

#### 3.9 Edge Agent 适配

- [ ] **7.9.1** 修改 Edge Agent 的 [`Initializer.cs`](../../src/HysteriaAuth.Agent/Services/Initializer.cs)：

> Edge Agent 注册成功后从 `RegisterWithTokenResponse` 或 `NodeConfigResponse` 中获取 `configYaml` 字段，写入 Hysteria 2 的配置文件。

```csharp
// 伪代码：Initializer 中处理配置同步
var configResponse = await FetchNodeConfig(nodeId, nodeSecret);

// 写入 Hysteria 2 配置文件
if (!string.IsNullOrWhiteSpace(configResponse.ConfigYaml))
{
    await File.WriteAllTextAsync("/etc/hysteria/config.yaml", configResponse.ConfigYaml);
    
    // 应用 iptables 端口跳跃规则
    if (configResponse.Config.EnablePortHopping)
    {
        ApplyPortHoppingRules(configResponse.Config.ListenPort ?? 6789,
                               configResponse.Config.PortHopRangeStart ?? 61000,
                               configResponse.Config.PortHopRangeEnd ?? 63000);
    }
    
    // 重载 Hysteria 2 服务
    await ReloadHysteriaService();
}
```

- [ ] **7.9.2** 新增 `ApplyPortHoppingRules` 方法：

```csharp
private static void ApplyPortHoppingRules(int listenPort, int rangeStart, int rangeEnd)
{
    // 检查规则是否已存在
    var checkCmd = $"iptables -t nat -C PREROUTING -i eth0 -p udp --dport {rangeStart}:{rangeEnd} -j DNAT --to-destination :{listenPort} 2>/dev/null";
    var checkResult = RunCommand(checkCmd);
    
    if (checkResult.ExitCode != 0)
    {
        // 规则不存在，添加
        var addCmd = $"iptables -t nat -A PREROUTING -i eth0 -p udp --dport {rangeStart}:{rangeEnd} -j DNAT --to-destination :{listenPort}";
        RunCommand(addCmd);
    }
}
```

- [ ] **7.9.3** 修改 Edge Agent 的心跳逻辑（[`StatusReporter.cs`](../../src/HysteriaAuth.Agent/Services/StatusReporter.cs)）：

> 心跳响应中增加 `configVersion` 字段对比。如果 Master 返回的 `configVersion` 高于本地记录的版本，触发配置重新同步。

```csharp
// 伪代码：心跳处理中增加配置版本检查
if (heartbeatResponse.ConfigVersion > localConfigVersion)
{
    _logger.LogInformation("检测到配置变更 (本地: {Local}, 远程: {Remote})，重新拉取配置",
        localConfigVersion, heartbeatResponse.ConfigVersion);
    await _initializer.SyncConfigAsync();
    localConfigVersion = heartbeatResponse.ConfigVersion;
}
```

- [ ] **7.9.4** 修改 Edge Agent 的模型类（[`AgentModels.cs`](../../src/HysteriaAuth.Agent/Models/AgentModels.cs)），增加 `ConfigYaml` 和 `ConfigVersion` 字段：

```csharp
public class NodeConfigResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ConfigVersion { get; set; }
    public string? ConfigYaml { get; set; }
    public NodeConfigInfo Config { get; set; } = new();
}
```

---

### 第 4 天：测试 + 验证

#### 3.10 单元测试 & 集成测试

- [ ] **7.10.1** 新增 `ConfigGeneratorServiceTests.cs`：

```csharp
// tests/HysteriaAuth.Tests/Unit/Services/ConfigGeneratorServiceTests.cs

[Fact]
public void GenerateYaml_PortHoppingEnabled_ProducesCorrectListen()
{
    var node = new Node
    {
        Id = "test-node-1",
        Name = "测试节点",
        SecretKey = "encrypted-secret",
        ListenAddress = "0.0.0.0",
        ListenPort = 6789,
        EnablePortHopping = true,
        PortHopRangeStart = 61000,
        PortHopRangeEnd = 63000,
        ConfigVersion = 1
    };

    var yaml = _service.GenerateYaml(node);

    Assert.Contains("listen: 0.0.0.0:6789,0.0.0.0:61000-63000", yaml);
}

[Fact]
public void GenerateYaml_PortHoppingDisabled_ProducesSinglePort()
{
    var node = new Node
    {
        Id = "test-node-2",
        Name = "测试节点",
        SecretKey = "encrypted-secret",
        ListenPort = 6789,
        EnablePortHopping = false,
        ConfigVersion = 1
    };

    var yaml = _service.GenerateYaml(node);

    Assert.Contains("listen: 0.0.0.0:6789", yaml);
    Assert.DoesNotContain("61000-63000", yaml);
}

[Fact]
public void GenerateYaml_WithObfs_ProducesObfsSection()
{
    var node = new Node
    {
        Id = "test-node-3",
        Name = "测试节点",
        SecretKey = "encrypted-secret",
        ObfsType = "salamander",
        ObfsPassword = "encrypted-password",
        ConfigVersion = 1
    };

    var yaml = _service.GenerateYaml(node);

    Assert.Contains("obfs:", yaml);
    Assert.Contains("type: salamander", yaml);
}
```

- [ ] **7.10.2** 新增 `UpdateNodeConfigIntegrationTests.cs`：

```csharp
// tests/HysteriaAuth.Tests/Integration/ApiTests/UpdateNodeConfigIntegrationTests.cs

[Fact]
public async Task UpdateNodeConfig_ValidAdmin_UpdatesFieldsAndIncrementsVersion()
{
    // 1. 创建测试节点
    // 2. 管理员认证获取 Token
    // 3. PUT /api/v1/admin/nodes/{nodeId}/config with { enablePortHopping: false, remark: "测试备注" }
    // 4. 断言 200 + ConfigVersion 递增
    // 5. 断言 Remark == "测试备注", EnablePortHopping == false
}

[Fact]
public async Task UpdateNodeConfig_Unauthorized_Returns401()
{
    // PUT /api/v1/admin/nodes/{nodeId}/config without auth header
    // 断言 401
}
```

- [ ] **7.10.3** 数据库迁移验证：

```bash
# 创建新节点，验证所有新字段可正常写入
# 读取现有节点，验证各字段默认值正确
```

#### 3.11 端到端验证

- [ ] **7.11.1** 预注册节点（通过 SPA 面板或 API），验证新字段 `ListenPort`、`DomainName`、`Remark` 已初始化
- [ ] **7.11.2** Edge Agent 使用预注册令牌注册，验证返回的 `configYaml` 字段非空且格式正确
- [ ] **7.11.3** 管理员修改节点配置（如启用/关闭端口跳跃、修改混淆密码），验证：
  - `ConfigVersion` 递增
  - 审计日志记录变更
  - Edge Agent 下一次心跳后自动拉取新配置
- [ ] **7.11.4** 验证端口跳跃功能：
  - Edge Agent 节点上执行 `iptables -t nat -L PREROUTING -n` 确认 DNAT 规则已添加
  - 使用客户端从 61000-63000 范围随机端口连接，验证 Hysteria 2 服务正常响应

---

## 4. 文件变更清单

| 文件 | 操作 | 说明 |
|------|------|------|
| [`src/HysteriaAuth.Master/Models/Entities/Node.cs`](../../src/HysteriaAuth.Master/Models/Entities/Node.cs) | 修改 | 追加 ~46 个新字段 |
| [`src/HysteriaAuth.Master/Migrations/*_ExpandNodeTable.cs`](../../src/HysteriaAuth.Master/Migrations/) | 新建 | EF Core Migration |
| [`src/HysteriaAuth.Master/Migrations/AppDbContextModelSnapshot.cs`](../../src/HysteriaAuth.Master/Migrations/AppDbContextModelSnapshot.cs) | 自动更新 | EF Core 自动重新生成 |
| [`src/HysteriaAuth.Master/Models/DTOs/NodeDto.cs`](../../src/HysteriaAuth.Master/Models/DTOs/NodeDto.cs) | 修改 | 扩展 `NodeDto`、`NodeConfigResponse`、`PreRegisterNodeRequest` |
| [`src/HysteriaAuth.Master/Models/DTOs/UpdateNodeConfigRequest.cs`](../../src/HysteriaAuth.Master/Models/DTOs/UpdateNodeConfigRequest.cs) | 新建 | 配置更新请求 DTO |
| [`src/HysteriaAuth.Master/Services/ConfigGeneratorService.cs`](../../src/HysteriaAuth.Master/Services/ConfigGeneratorService.cs) | 新建 | YAML 配置生成器 |
| [`src/HysteriaAuth.Master/Services/NodeService.cs`](../../src/HysteriaAuth.Master/Services/NodeService.cs) | 修改 | 注入 `ConfigGeneratorService`；新增 `UpdateNodeConfigAsync`；扩展 `MapToDto` |
| [`src/HysteriaAuth.Master/Controllers/NodesController.cs`](../../src/HysteriaAuth.Master/Controllers/NodesController.cs) | 修改 | 新增 `PUT /api/v1/admin/nodes/{nodeId}/config` |
| [`src/HysteriaAuth.Master/Program.cs`](../../src/HysteriaAuth.Master/Program.cs) | 修改 | 注册 `ConfigGeneratorService` |
| [`src/HysteriaAuth.Agent/Services/Initializer.cs`](../../src/HysteriaAuth.Agent/Services/Initializer.cs) | 修改 | 解析 `configYaml` 并写入 Hysteria 2 配置文件 |
| [`src/HysteriaAuth.Agent/Services/StatusReporter.cs`](../../src/HysteriaAuth.Agent/Services/StatusReporter.cs) | 修改 | 心跳响应中检查 `configVersion`，触发配置重同步 |
| [`src/HysteriaAuth.Agent/Models/AgentModels.cs`](../../src/HysteriaAuth.Agent/Models/AgentModels.cs) | 修改 | 增加 `ConfigYaml`、`ConfigVersion` 字段 |
| [`tests/HysteriaAuth.Tests/Unit/Services/ConfigGeneratorServiceTests.cs`](../../tests/HysteriaAuth.Tests/Unit/Services/ConfigGeneratorServiceTests.cs) | 新建 | 配置生成器单元测试 |
| [`tests/HysteriaAuth.Tests/Integration/ApiTests/UpdateNodeConfigIntegrationTests.cs`](../../tests/HysteriaAuth.Tests/Integration/ApiTests/UpdateNodeConfigIntegrationTests.cs) | 新建 | 配置更新 API 集成测试 |

---

## 5. 注意事项

| # | 注意事项 | 说明 |
|---|----------|------|
| 1 | **向后兼容** | 所有新字段均 NULLABLE 或有默认值，Migration 仅执行 ADD COLUMN，现有数据和 API 完全不受影响 |
| 2 | **`Port` 字段保留** | 保留原有 `Port` 列，`ConfigGeneratorService` 优先使用 `ListenPort`（非 NULL 时），回退到 `Port` |
| 3 | **AES 加密一致性** | `ObfsPassword` 字段写入时必须通过 `_aes.Encrypt()` 加密，读取时必须通过 `_aes.Decrypt()` 解密 |
| 4 | **配置版本号递增** | 每次配置变更（`UpdateNodeConfigAsync`）必须 `ConfigVersion++`，不得跳过 |
| 5 | **iptables 规则幂等** | Edge Agent 应用 iptables 规则前先检查是否已存在，避免重复添加 |
| 6 | **YAML 缩进** | `ConfigGeneratorService` 使用 2 空格缩进，与 Hysteria 2 官方文档一致 |
| 7 | **空值处理** | NULL 的配置字段在 YAML 生成时直接省略对应配置段，不输出空对象 `{}` |
| 8 | **测试数据库** | 集成测试应使用独立 SQLite 数据库（`:memory:` 或临时文件），不影响开发数据库 |

---

## 6. 完成标准

| # | 标准 | 验证方式 |
|---|------|---------|
| C1 | `Node` Entity 包含全部 ~60 个字段 | 检查 `Node.cs` 文件，对比 `node-table-redesign.md` §4 |
| C2 | Migration 成功应用，无报错 | `dotnet ef database update` 无错误退出 |
| C3 | 现有节点数据未受影响 | 查询一条现有节点记录，原有字段值完整 |
| C4 | `GET /api/v1/nodes/{nodeId}/config` 返回 `configYaml` 非空 | 用 Edge Agent 注册后，调用配置同步 API，验证响应包含有效 YAML |
| C5 | `PUT /api/v1/admin/nodes/{nodeId}/config` 接受并应用配置变更 | 修改 `EnablePortHopping=false`，验证 `ConfigVersion` 递增 + YAML 中 `listen` 仅单端口 |
| C6 | 配置变更写入审计日志 | 查询 `AdminAuditLogs` 表，存在 `action=update_config` 记录 |
| C7 | Edge Agent 启动后正确应用 iptables 规则 | `iptables -t nat -L PREROUTING -n` 包含端口跳跃规则 |
| C8 | 单元测试通过 | `dotnet test --filter Category=ConfigGenerator` 全部通过 |
| C9 | 集成测试通过 | `dotnet test --filter Category=Integration` 全部通过 |

---

## 7. 阶段交接清单

| # | 交付物 | 接收方 |
|---|--------|--------|
| H1 | 更新后的 `Node.cs` Entity 文件 | 后端开发 |
| H2 | EF Core Migration 文件 | 后端开发 / DBA |
| H3 | `UpdateNodeConfigRequest.cs` DTO | 前端开发（SPA 表单构建） |
| H4 | `ConfigGeneratorService` | Edge Agent 开发 |
| H5 | 新增 `PUT /api/v1/admin/nodes/{nodeId}/config` 端点 | 前端开发（SPA 集成） |
| H6 | 更新后的 `NodeDto`（含运营字段） | 前端开发（节点列表/详情页） |
| H7 | SPA 节点配置编辑页面规格 | 前端开发 |
| H8 | Edge Agent 配置同步 + iptables 逻辑 | Edge Agent 开发 |

---

## 8. 关联文档

| 文档 | 路径 |
|------|------|
| 节点数据表重新设计 | [`document/architect/node-table-redesign.md`](../architect/node-table-redesign.md) |
| Hysteria 2 完整配置参考 | [`document/hysteria/hysteria-server-config.md`](../hysteria/hysteria-server-config.md) |
| 数据库设计（原始） | [`document/architect/database-design.md`](../architect/database-design.md) |
| 边缘节点设计 | [`document/architect/edge-node-design.md`](../architect/edge-node-design.md) |
| API 设计 | [`document/architect/api-design.md`](../architect/api-design.md) |
| 后端开发规范 | [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md) |
| 管理员面板 API | [`document/exchange/master-panel-api.md`](../exchange/master-panel-api.md) |
