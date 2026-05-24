using Microsoft.AspNetCore.Mvc;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Services;

namespace HysteriaAuth.Master.Controllers;

/// <summary>
/// 节点管理控制器 — Phase 2/3/7。
/// 提供节点预注册、注册（令牌+旧版）、配置同步、列表/详情/历史状态、心跳上报、密钥轮换、配置更新。
/// </summary>
[ApiController]
[Route("api/v1")]
public class NodesController : ControllerBase
{
    private readonly NodeService _nodeService;
    private readonly ILogger<NodesController> _logger;

    public NodesController(NodeService nodeService, ILogger<NodesController> logger)
    {
        _nodeService = nodeService;
        _logger = logger;
    }

    // ============================
    // 3.1 节点预注册（管理员侧）
    // ============================

    /// <summary>
    /// 管理员预注册节点，生成预注册令牌供 Edge Agent 首次注册使用。
    /// </summary>
    [HttpPost("admin/nodes/pre-register")]
    public async Task<IActionResult> PreRegisterNode([FromBody] PreRegisterNodeRequest request)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var result = await _nodeService.PreRegisterNodeAsync(request, adminId, clientIp);
        return Created(string.Empty, result);
    }

    // ============================
    // 3.2 节点注册
    // ============================

    /// <summary>
    /// 令牌注册（推荐方式）— Edge Agent 使用预注册令牌向主服务器注册并获取配置。
    /// </summary>
    [HttpPost("nodes/register-with-token")]
    public async Task<IActionResult> RegisterWithToken([FromBody] RegisterWithTokenRequest request)
    {
        var result = await _nodeService.RegisterWithTokenAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 旧版注册（保留兼容）— Edge Agent 直接注册节点信息。
    /// </summary>
    [HttpPost("nodes/register")]
    public async Task<IActionResult> RegisterNode([FromBody] RegisterNodeRequest request)
    {
        var result = await _nodeService.RegisterNodeAsync(request);
        return Ok(result);
    }

    // ============================
    // 3.3 节点配置同步
    // ============================

    /// <summary>
    /// 获取节点最新配置（Edge Agent 启动时或心跳前同步使用）。
    /// Phase 7: 返回完整 Hysteria 2 YAML 配置。
    /// </summary>
    [HttpGet("nodes/{nodeId}/config")]
    public async Task<IActionResult> GetNodeConfig(string nodeId)
    {
        var result = await _nodeService.GetNodeConfigAsync(nodeId);
        return Ok(result);
    }

    // ============================
    // 3.4 节点列表/详情/历史状态
    // ============================

    /// <summary>
    /// 获取节点列表（管理员认证）。
    /// </summary>
    [HttpGet("nodes")]
    public async Task<IActionResult> GetNodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        var result = await _nodeService.GetNodesAsync(page, pageSize, isActive);
        return Ok(result);
    }

    /// <summary>
    /// 获取节点详情（管理员认证）。
    /// </summary>
    [HttpGet("nodes/{nodeId}")]
    public async Task<IActionResult> GetNodeDetail(string nodeId)
    {
        var result = await _nodeService.GetNodeDetailAsync(nodeId);
        return Ok(result);
    }

    /// <summary>
    /// 获取节点历史状态（管理员认证）。
    /// </summary>
    [HttpGet("nodes/{nodeId}/status-history")]
    public async Task<IActionResult> GetNodeStatusHistory(
        string nodeId,
        [FromQuery] int hours = 24)
    {
        var result = await _nodeService.GetNodeStatusHistoryAsync(nodeId, hours);
        return Ok(result);
    }

    // ============================
    // 3.7 心跳上报
    // ============================

    /// <summary>
    /// 边缘节点心跳/状态上报（含系统状态、流量数据）。
    /// Phase 7: 响应中返回 configVersion 供 Edge Agent 检测配置变更。
    /// </summary>
    [HttpPost("nodes/{nodeId}/heartbeat")]
    public async Task<IActionResult> PostHeartbeat(string nodeId, [FromBody] HeartbeatRequest request)
    {
        // 确保心跳请求中的 nodeId 与路由一致
        if (request.NodeId != nodeId)
        {
            request.NodeId = nodeId;
        }

        await _nodeService.ProcessHeartbeatAsync(request);

        // Phase 7: 返回 configVersion 供 Edge Agent 检测配置变更
        var configVersion = await _nodeService.GetNodeConfigVersionAsync(nodeId);

        return Ok(new HeartbeatResponse
        {
            Received = true,
            ConfigVersion = configVersion
        });
    }

    // ============================
    // Phase 7: 管理员更新节点配置
    // ============================

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

    // ============================
    // 3.9 节点密钥轮换
    // ============================

    /// <summary>
    /// 轮换节点密钥（管理员认证）— 生成新密钥，递增 SecretVersion。
    /// </summary>
    [HttpPost("admin/nodes/{nodeId}/rotate-secret")]
    public async Task<IActionResult> RotateSecret(string nodeId)
    {
        var adminId = (long)HttpContext.Items["AdminId"]!;
        var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
        var result = await _nodeService.RotateSecretAsync(nodeId, adminId, clientIp);
        return Ok(result);
    }
}
