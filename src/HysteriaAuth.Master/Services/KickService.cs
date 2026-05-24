using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// 踢用户下线服务 — Phase 3 核心交付。
/// 主服务器检测流量超额后，通过 Node 表中的 IpAddress 直接调用 Edge Agent 的 /kick-user 端点，
/// Agent 再转发到本地 Hysteria POST /kick。
/// 同时禁用用户 IsActive 以防 Hysteria 客户端自动重连。
/// </summary>
public class KickService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserRepository _userRepo;
    private readonly INodeRepository _nodeRepo;
    private readonly ILogger<KickService> _logger;
    private readonly AuditService _auditService;

    // Agent 监听 AuthProxy 的端口（/kick-user 端点注册在同一端口上）
    private const int AgentPort = 8080;

    public KickService(
        IHttpClientFactory httpClientFactory,
        IUserRepository userRepo,
        INodeRepository nodeRepo,
        ILogger<KickService> logger,
        AuditService auditService)
    {
        _httpClientFactory = httpClientFactory;
        _userRepo = userRepo;
        _nodeRepo = nodeRepo;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// 踢指定用户下线。通过 Edge Agent 调用 Hysteria POST /kick。
    /// 同时将用户 IsActive 设为 false 以防自动重连。
    /// </summary>
    public async Task KickUserAsync(string username, string nodeId)
    {
        // 1. 查找节点 IP，直接调用 Agent 的 /kick-user 端点
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null || string.IsNullOrWhiteSpace(node.IpAddress))
        {
            _logger.LogWarning("无法踢用户下线，节点不存在或无 IP: NodeId={NodeId}", nodeId);
            return;
        }

        try
        {
            var agentUrl = $"http://{node.IpAddress}:{AgentPort}/kick-user";
            var client = _httpClientFactory.CreateClient();
            var payload = new KickUserRequest { Username = username, NodeId = nodeId };

            var response = await client.PostAsJsonAsync(agentUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("用户 {Username} 已从节点 {NodeId} 踢下线", username, nodeId);
            }
            else
            {
                _logger.LogWarning("踢用户下线失败 (HTTP {Code}): {Username}@{NodeId}",
                    (int)response.StatusCode, username, nodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "踢用户下线网络异常: {Username}@{NodeId} → {IpAddress}",
                username, nodeId, node.IpAddress);
        }

        // 2. 禁用用户账号，防止 Hysteria 客户端自动重连
        try
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user != null && user.IsActive)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
                _logger.LogWarning("用户 {Username} 账号已禁用（流量超额）", username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用用户账号失败: {Username}", username);
        }
    }

    /// <summary>
    /// 管理员主动踢人（通过 API 调用）。
    /// </summary>
    public async Task AdminKickUserAsync(KickUserRequest request, long adminId, string clientIp)
    {
        var user = await _userRepo.GetByUsernameAsync(request.Username);
        if (user == null)
            throw new NotFoundException("用户不存在");

        await KickUserAsync(request.Username, request.NodeId);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "kick_user",
            targetType: "user",
            targetId: request.Username,
            detail: new
            {
                nodeId = request.NodeId,
                username = request.Username
            },
            clientIp: clientIp
        );
    }
}
