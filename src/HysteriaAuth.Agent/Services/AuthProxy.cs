using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// Edge Agent 认证代理服务。监听本地 HTTP 端口，接收 Hysteria 原生 POST /auth 请求，
/// 完成 Hysteria 原生协议 → 项目内部协议的转换后转发到主服务器。
/// </summary>
public class AuthProxy
{
    private readonly AgentConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthProxy> _logger;

    public AuthProxy(AgentConfig config, ILogger<AuthProxy> logger)
    {
        _config = config;
        _httpClient = new HttpClient();
        _logger = logger;
    }

    /// <summary>
    /// 处理 Hysteria 原生认证请求，执行协议转换。
    /// </summary>
    public async Task<HysteriaAuthResponse> HandleAuthAsync(HysteriaAuthRequest request)
    {
        _logger.LogDebug("收到 Hysteria 认证请求: Addr={Addr}, Auth={AuthPrefix}...",
            request.Addr, request.Auth.Length > 10 ? request.Auth[..10] : request.Auth);

        // 1. 解析 auth 字段（Base64 编码的凭据串 → username + password）
        var (username, password) = ParseAuthField(request.Auth);

        // 2. 提取客户端 IP
        var clientIp = ExtractIp(request.Addr);

        // 3. 构造内部 API 请求
        var internalRequest = new InternalAuthRequest
        {
            Username = username,
            Password = password,
            NodeId = _config.NodeId,
            ClientIp = clientIp
        };

        // 4. 转发到主服务器
        try
        {
            var requestMsg = new HttpRequestMessage(HttpMethod.Post,
                $"{_config.MasterServerUrl}/api/v1/auth/hysteria");
            requestMsg.Headers.Add("X-Node-Secret", _config.NodeSecret);
            requestMsg.Content = JsonContent.Create(internalRequest);

            var response = await _httpClient.SendAsync(requestMsg);

            // 5. 转换响应为 Hysteria 原生格式
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<InternalAuthResponse>();
                _logger.LogInformation("认证成功: Username={Username}, UserId={UserId}",
                    username, body?.UserId);

                return new HysteriaAuthResponse { Ok = true, Id = username };
            }

            _logger.LogWarning("主服务器认证失败: Username={Username}, StatusCode={StatusCode}",
                username, (int)response.StatusCode);
            return new HysteriaAuthResponse { Ok = false, Id = "" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用主服务器认证 API 失败: {Message}", ex.Message);
            return new HysteriaAuthResponse { Ok = false, Id = "" };
        }
    }

    /// <summary>
    /// 解析 Hysteria auth 字段。auth 为 Base64 编码的凭据字符串，解码后按 ':' 分割为 username:password。
    /// 若不含 ':' 分隔符，则将整个字符串作为 username，password 为空字符串。
    /// </summary>
    private static (string username, string password) ParseAuthField(string auth)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
            var parts = decoded.Split(':', 2);
            return parts.Length == 2
                ? (parts[0], parts[1])
                : (decoded, "");
        }
        catch (FormatException)
        {
            // 如果 auth 不是有效 Base64，尝试直接按 : 分割
            var parts = auth.Split(':', 2);
            return parts.Length == 2
                ? (parts[0], parts[1])
                : (auth, "");
        }
    }

    /// <summary>
    /// 从 "IP:Port" 或 "[IPv6]:Port" 格式中提取 IP 部分。
    /// </summary>
    private static string ExtractIp(string addr)
    {
        // 处理 IPv6: "[::1]:12345" → "::1"
        if (addr.StartsWith("["))
        {
            var closeBracket = addr.IndexOf(']');
            if (closeBracket > 0)
                return addr[1..closeBracket];
            return addr;
        }

        // 处理 IPv4: "1.2.3.4:12345" → "1.2.3.4"
        var lastColon = addr.LastIndexOf(':');
        if (lastColon < 0) return addr;
        return addr[..lastColon];
    }
}
