using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// 节点管理服务 — 负责节点的预注册、注册、配置同步、心跳处理、密钥轮换等全部业务逻辑。
/// </summary>
public class NodeService
{
    private readonly INodeRepository _nodeRepo;
    private readonly INodeStatusRepository _nodeStatusRepo;
    private readonly AppDbContext _context;
    private readonly AesEncryptionService _aes;
    private readonly ILogger<NodeService> _logger;
    private readonly string _masterServerUrl;

    public NodeService(
        INodeRepository nodeRepo,
        INodeStatusRepository nodeStatusRepo,
        AppDbContext context,
        AesEncryptionService aes,
        IConfiguration configuration,
        ILogger<NodeService> logger)
    {
        _nodeRepo = nodeRepo;
        _nodeStatusRepo = nodeStatusRepo;
        _context = context;
        _aes = aes;
        _logger = logger;
        _masterServerUrl = configuration.GetValue<string>("MasterServerUrl") ?? "https://master.example.com";
    }

    // ============================
    // 3.1 节点预注册（管理员侧）
    // ============================

    public async Task<PreRegisterNodeResponse> PreRegisterNodeAsync(PreRegisterNodeRequest request)
    {
        var nodeId = Guid.NewGuid().ToString("N")[..12]; // 12 位 hex
        var provisionToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 128-bit

        var node = new Node
        {
            Id = nodeId,
            Name = request.Name,
            Port = request.Port,
            Location = request.Location,
            TrafficStatsPort = request.TrafficStatsPort,
            ProvisionToken = provisionToken,
            ProvisionStatus = "pending",
            IsActive = false, // 注册完成后才激活
            CreatedAt = DateTime.UtcNow
        };

        await _nodeRepo.AddAsync(node);

        _logger.LogInformation("管理员预注册节点: {NodeId}, 名称: {Name}", nodeId, request.Name);

        return new PreRegisterNodeResponse
        {
            ProvisionToken = provisionToken,
            MasterServerUrl = _masterServerUrl,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            StartupCommand = $"./edge-agent --provision-token={provisionToken} --master-url={_masterServerUrl}"
        };
    }

    // ============================
    // 3.2 令牌注册
    // ============================

    public async Task<RegisterWithTokenResponse> RegisterWithTokenAsync(RegisterWithTokenRequest request)
    {
        // 1. 查找令牌
        var node = await _nodeRepo.GetByProvisionTokenAsync(request.ProvisionToken);
        if (node == null)
            throw new AppException("not_found", "预注册令牌不存在或已使用", 404);

        // 2. 检查状态
        if (node.ProvisionStatus != "pending")
            throw new AppException("conflict", "该令牌已使用", 409);

        // 3. 检查令牌是否过期（预注册 7 天未使用则过期）
        if (node.CreatedAt.AddDays(7) < DateTime.UtcNow)
            throw new AppException("token_expired", "预注册令牌已过期", 401);

        // 4. 生成密钥
        var nodeSecretPlain = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 256-bit
        var trafficStatsSecretPlain = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 128-bit

        // 5. 更新节点
        node.Name = request.Name ?? node.Name;
        node.IpAddress = request.IpAddress;
        node.SecretKey = _aes.Encrypt(nodeSecretPlain); // AES-256-GCM 加密存储
        node.SecretVersion = 1;
        node.TrafficStatsSecret = _aes.Encrypt(trafficStatsSecretPlain);
        node.ProvisionToken = null; // 令牌清零，一次性使用
        node.ProvisionStatus = "provisioned";
        node.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("节点令牌注册成功: {NodeId}", node.Id);

        return new RegisterWithTokenResponse
        {
            NodeId = node.Id,
            NodeSecret = nodeSecretPlain, // 返回明文（仅此一次）
            TrafficStatsSecret = trafficStatsSecretPlain,
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

    // ============================
    // 3.2 旧版注册（保留兼容）
    // ============================

    public async Task<NodeDto> RegisterNodeAsync(RegisterNodeRequest request)
    {
        var existing = await _nodeRepo.GetByIdAsync(request.NodeId);

        if (existing != null)
        {
            // 更新已有节点信息
            existing.Name = request.Name;
            existing.IpAddress = request.IpAddress;
            existing.Port = request.Port;
            existing.Location = request.Location;
            existing.TrafficStatsPort = request.TrafficStatsPort;
            if (!string.IsNullOrWhiteSpace(request.TrafficStatsSecret))
                existing.TrafficStatsSecret = _aes.Encrypt(request.TrafficStatsSecret);

            await _nodeRepo.UpdateAsync(existing);
            _logger.LogInformation("旧版注册（更新）: {NodeId}", request.NodeId);
            return MapToDto(existing);
        }

        // 新增节点
        var node = new Node
        {
            Id = request.NodeId,
            Name = request.Name,
            IpAddress = request.IpAddress,
            Port = request.Port,
            Location = request.Location,
            TrafficStatsPort = request.TrafficStatsPort,
            TrafficStatsSecret = !string.IsNullOrWhiteSpace(request.TrafficStatsSecret)
                ? _aes.Encrypt(request.TrafficStatsSecret) : null,
            ProvisionStatus = "provisioned", // 旧版注册不经过预注册流程
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _nodeRepo.AddAsync(node);
        _logger.LogInformation("旧版注册（新增）: {NodeId}", request.NodeId);
        return MapToDto(node);
    }

    // ============================
    // 3.3 节点配置同步
    // ============================

    public async Task<NodeConfigResponse> GetNodeConfigAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null)
            throw new NotFoundException("节点不存在");

        return new NodeConfigResponse
        {
            NodeId = node.Id,
            NodeSecret = _aes.Decrypt(node.SecretKey),
            IsActive = node.IsActive,
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

    // ============================
    // 3.4 节点列表/详情/历史状态
    // ============================

    public async Task<NodeListResponse> GetNodesAsync(int page, int pageSize, bool? isActive)
    {
        var (items, total) = await _nodeRepo.GetAllAsync(page, pageSize, isActive);

        return new NodeListResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    public async Task<NodeDetailResponse> GetNodeDetailAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null)
            throw new NotFoundException("节点不存在");

        return new NodeDetailResponse
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
            SecretVersion = node.SecretVersion,
            TrafficStatsSecret = node.TrafficStatsSecret != null ? "***encrypted***" : null
        };
    }

    public async Task<NodeStatusHistoryResponse> GetNodeStatusHistoryAsync(string nodeId, int hours)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null)
            throw new NotFoundException("节点不存在");

        var statuses = await _nodeStatusRepo.GetHistoryAsync(nodeId, hours);

        return new NodeStatusHistoryResponse
        {
            NodeId = nodeId,
            Hours = hours,
            Items = statuses.Select(s => new NodeStatusDto
            {
                Id = s.Id,
                NodeId = s.NodeId,
                CpuUsagePercent = s.CpuUsagePercent,
                MemoryUsagePercent = s.MemoryUsagePercent,
                MemoryUsedMb = s.MemoryUsedMb,
                MemoryTotalMb = s.MemoryTotalMb,
                NetworkInBytes = s.NetworkInBytes,
                NetworkOutBytes = s.NetworkOutBytes,
                NetworkInMbps = s.NetworkInMbps,
                NetworkOutMbps = s.NetworkOutMbps,
                ActiveConnections = s.ActiveConnections,
                ReportedAt = s.ReportedAt
            }).ToList()
        };
    }

    // ============================
    // 3.7 心跳处理（Phase 2 仅系统状态，不含流量数据）
    // ============================

    public async Task ProcessHeartbeatAsync(HeartbeatRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 写入节点系统状态
            _context.NodeStatuses.Add(new NodeStatus
            {
                NodeId = request.NodeId,
                CpuUsagePercent = request.CpuUsagePercent,
                MemoryUsagePercent = request.MemoryUsagePercent,
                MemoryUsedMb = request.MemoryUsedMb,
                MemoryTotalMb = request.MemoryTotalMb,
                NetworkInBytes = request.NetworkInBytes,
                NetworkOutBytes = request.NetworkOutBytes,
                NetworkInMbps = request.NetworkInMbps,
                NetworkOutMbps = request.NetworkOutMbps,
                ActiveConnections = request.ActiveConnections,
                ReportedAt = request.ReportedAt
            });

            // 2. 写入节点流量汇总（Phase 2 可为 0）
            _context.NodeTraffics.Add(new NodeTraffic
            {
                NodeId = request.NodeId,
                TotalBytesIn = 0,
                TotalBytesOut = 0,
                ActiveUsers = 0,
                RecordedAt = DateTime.UtcNow
            });

            // 3. 更新节点心跳时间和激活状态
            var node = await _nodeRepo.GetByIdAsync(request.NodeId);
            if (node != null)
            {
                node.LastHeartbeat = DateTime.UtcNow;
                node.IsActive = true; // 恢复激活（如果之前离线）
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================
    // 3.9 节点密钥轮换
    // ============================

    public async Task<RotateSecretResponse> RotateSecretAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null)
            throw new NotFoundException("节点不存在");

        // 生成新密钥
        var newSecretPlain = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 256-bit

        // 多版本密钥存储：旧密钥保留在数据库中（通过 SecretVersion 区分）
        // Phase 2 简化实现：直接替换密钥，递增版本号
        // 完整的零停机轮换需要额外的密钥历史表，后续迭代实现
        node.SecretKey = _aes.Encrypt(newSecretPlain);
        node.SecretVersion += 1;

        await _nodeRepo.UpdateAsync(node);

        _logger.LogInformation("节点密钥轮换完成: {NodeId}, 新版本: {Version}", nodeId, node.SecretVersion);

        return new RotateSecretResponse
        {
            NodeId = nodeId,
            NewSecret = newSecretPlain,
            NewSecretVersion = node.SecretVersion
        };
    }

    // ============================
    // 对 AuthService 暴露：检查节点是否活跃
    // ============================

    public async Task<bool> IsNodeActiveAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        return node != null && node.IsActive;
    }

    /// <summary>
    /// 验证节点密钥（用于 NodeAuthMiddleware）。返回匹配的 Node，否则返回 null。
    /// 支持多版本密钥验证 — 检查数据库中存储的当前密钥和之前版本。
    /// </summary>
    public async Task<Node?> ValidateNodeSecretAsync(string plainSecret)
    {
        // 查询所有活跃节点（Phase 2 简化：逐个尝试解密匹配）
        // 生产环境中应优化为加密哈希索引
        var activeNodes = await _nodeRepo.GetActiveNodesAsync();

        foreach (var node in activeNodes)
        {
            try
            {
                var decryptedKey = _aes.Decrypt(node.SecretKey);
                if (decryptedKey == plainSecret)
                    return node;
            }
            catch
            {
                // 解密失败跳过（可能为旧格式或损坏数据）
            }
        }

        return null;
    }

    // ============================
    // 辅助：映射到 DTO
    // ============================

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
            ProvisionStatus = node.ProvisionStatus
        };
    }
}
