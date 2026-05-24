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

        // 8. auth（HTTP 认证指向 Master）
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

    private static void GenerateListen(System.Text.StringBuilder sb, Node node)
    {
        var addr = node.ListenAddress ?? "0.0.0.0";
        var port = node.ListenPort ?? node.Port;

        // if (node.EnablePortHopping && node.PortHopRangeStart.HasValue && node.PortHopRangeEnd.HasValue)
        // {
        //     sb.AppendLine($"listen: {addr}:{port},{addr}:{node.PortHopRangeStart}-{node.PortHopRangeEnd}");
        // }
        // else
        // {
        //     sb.AppendLine($"listen: {addr}:{port}");
        // }
        sb.AppendLine($"listen: {addr}:{port}");
        sb.AppendLine();
    }

    private void GenerateObfs(System.Text.StringBuilder sb, Node node)
    {
        if (string.IsNullOrWhiteSpace(node.ObfsType) || string.IsNullOrWhiteSpace(node.ObfsPassword))
            return;

        string password;
        try
        {
            password = _aes.Decrypt(node.ObfsPassword);
        }
        catch
        {
            // 如果解密失败（可能是明文存储），直接使用原值
            password = node.ObfsPassword;
        }

        sb.AppendLine("obfs:");
        sb.AppendLine($"  type: {node.ObfsType}");
        sb.AppendLine($"  {node.ObfsType}:");
        sb.AppendLine($"    password: \"{password}\"");
        sb.AppendLine();
    }

    private static void GenerateQuic(System.Text.StringBuilder sb, Node node)
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

        if (node.QuicInitStreamReceiveWindow.HasValue || node.QuicMaxStreamReceiveWindow.HasValue)
        {
            sb.AppendLine($"  initStreamReceiveWindow: {node.QuicInitStreamReceiveWindow ?? 8388608}");
            sb.AppendLine($"  maxStreamReceiveWindow: {node.QuicMaxStreamReceiveWindow ?? 8388608}");
        }
        if (node.QuicInitConnectionReceiveWindow.HasValue || node.QuicMaxConnectionReceiveWindow.HasValue)
        {
            sb.AppendLine($"  initConnectionReceiveWindow: {node.QuicInitConnectionReceiveWindow ?? 8388608}");
            sb.AppendLine($"  maxConnectionReceiveWindow: {node.QuicMaxConnectionReceiveWindow ?? 8388608}");
        }
        sb.AppendLine();
    }

    private static void GenerateBandwidth(System.Text.StringBuilder sb, Node node)
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

    private static void GenerateSpeedTest(System.Text.StringBuilder sb, Node node)
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

    private static void GenerateUdp(System.Text.StringBuilder sb, Node node)
    {
        if (node.UdpIdleTimeout.HasValue)
        {
            sb.AppendLine($"udpIdleTimeout: {node.UdpIdleTimeout}s");
            sb.AppendLine();
        }
    }

    private void GenerateAuth(System.Text.StringBuilder sb, Node node, string masterHost)
    {
        string nodeSecret;
        try
        {
            nodeSecret = _aes.Decrypt(node.SecretKey);
        }
        catch
        {
            nodeSecret = node.SecretKey;
        }

        sb.AppendLine("auth:");
        sb.AppendLine("  type: http");
        sb.AppendLine("  http:");
        sb.AppendLine($"    url: {masterHost.TrimEnd('/')}/api/v1/auth?nodeId={node.Id}");
        sb.AppendLine($"    secret: \"{nodeSecret}\"");
        sb.AppendLine();
    }

    private static void GenerateResolver(System.Text.StringBuilder sb, Node node)
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

    private static void GenerateSniff(System.Text.StringBuilder sb, Node node)
    {
        if (node.SniffEnabled != true) return;

        sb.AppendLine("sniff:");
        if (node.SniffTimeout.HasValue)
            sb.AppendLine($"  timeout: {node.SniffTimeout}s");
        if (node.SniffRespectHttps == true)
            sb.AppendLine("  respectHTTPS: true");
        sb.AppendLine();
    }

    private static void GenerateMasquerade(System.Text.StringBuilder sb, Node node)
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
        {
            string secret;
            try
            {
                secret = _aes.Decrypt(node.TrafficStatsSecret);
            }
            catch
            {
                secret = node.TrafficStatsSecret;
            }
            sb.AppendLine($"  secret: \"{secret}\"");
        }
        sb.AppendLine();
    }
}
