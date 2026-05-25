namespace HysteriaAuth.Master.Config;

/// <summary>
/// HTTPS 证书自动检测与监听端口配置。
/// 启动时自动检查 CertDirectoryPath 目录下是否存在公钥/私钥文件，
/// 存在则在 ListenPort 上启用 HTTPS，否则在 ListenPort 上回退 HTTP 并输出安全警告。
/// </summary>
public class HttpsSettings
{
    public const string SectionName = "Https";

    /// <summary>
    /// Kestrel 监听的网卡地址。默认 "0.0.0.0"（监听所有网卡，IPv4 + IPv6）。
    /// 可设为 "127.0.0.1" 仅本地回环（配合 Nginx 反向代理时）。
    /// </summary>
    public string ListenAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// Kestrel 监听的端口号。默认 5000。
    /// 无论最终使用 HTTP 还是 HTTPS，都在此端口上提供服务。
    /// </summary>
    public int ListenPort { get; set; } = 5000;

    /// <summary>
    /// 证书文件夹路径。支持相对路径（相对于 ContentRootPath）和绝对路径。
    /// 默认值 "cert"，即程序同级目录下的 cert/ 文件夹。
    /// </summary>
    public string CertDirectoryPath { get; set; } = "cert";

    /// <summary>
    /// HTTPS 公钥证书文件名。默认 "server.cert"。
    /// </summary>
    public string CertFileName { get; set; } = "server.cert";

    /// <summary>
    /// HTTPS 私钥文件名。默认 "server.key"。
    /// </summary>
    public string CertKeyFileName { get; set; } = "server.key";
}
