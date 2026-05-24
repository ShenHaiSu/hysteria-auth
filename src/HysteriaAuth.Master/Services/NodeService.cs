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
/// Phase 3: 心跳处理扩展为包含完整的流量扣减、幂等检查、会话管理、超额踢人。
/// Phase 7: 新增 ConfigGeneratorService 注入，支持完整 Hysteria 2 YAML 配置生成与更新。
/// </summary>
public class NodeService
{
    private readonly INodeRepository _nodeRepo;
    private readonly INodeStatusRepository _nodeStatusRepo;
    private readonly ITrafficRepository _trafficRepo;
    private readonly AppDbContext _context;
    private readonly AesEncryptionService _aes;
    private readonly TrafficService _trafficService;
    private readonly KickService _kickService;
    private readonly ConfigGeneratorService _configGenerator;
    private readonly ILogger<NodeService> _logger;
    private readonly string _masterServerUrl;
    private readonly AuditService _auditService;

    public NodeService(
        INodeRepository nodeRepo,
        INodeStatusRepository nodeStatusRepo,
        ITrafficRepository trafficRepo,
        AppDbContext context,
        AesEncryptionService aes,
        TrafficService trafficService,
        KickService kickService,
        ConfigGeneratorService configGenerator,
        IConfiguration configuration,
        ILogger<NodeService> logger,
        AuditService auditService)
    {
        _nodeRepo = nodeRepo;
        _nodeStatusRepo = nodeStatusRepo;
        _trafficRepo = trafficRepo;
        _context = context;
        _aes = aes;
        _trafficService = trafficService;
        _kickService = kickService;
        _configGenerator = configGenerator;
        _logger = logger;
        _masterServerUrl = configuration.GetValue<string>("MasterServerUrl") ?? "https://master.example.com";
        _auditService = auditService;
    }

    // ============================
    // 3.1 节点预注册（管理员侧）
    // ============================

    public async Task<PreRegisterNodeResponse> PreRegisterNodeAsync(PreRegisterNodeRequest request, long adminId, string clientIp)
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
            CreatedAt = DateTime.UtcNow,
            // Phase 7 新字段初始化
            ListenPort = request.ListenPort ?? 6789,
            DomainName = request.DomainName,
            Remark = request.Remark
        };

        await _nodeRepo.AddAsync(node);

        _logger.LogInformation("管理员预注册节点: {NodeId}, 名称: {Name}", nodeId, request.Name);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "create",
            targetType: "node",
            targetId: nodeId,
            detail: new
            {
                name = request.Name,
                port = request.Port,
                location = request.Location,
                trafficStatsPort = request.TrafficStatsPort,
                listenPort = request.ListenPort,
                domainName = request.DomainName,
                remark = request.Remark
            },
            clientIp: clientIp
        );

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

        // Phase 7: 生成完整 Hysteria 2 YAML 配置
        var configYaml = _configGenerator.GenerateYaml(node);

        return new RegisterWithTokenResponse
        {
            NodeId = node.Id,
            NodeSecret = nodeSecretPlain, // 返回明文（仅此一次）
            TrafficStatsSecret = trafficStatsSecretPlain,
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

        // Phase 7: 生成完整 Hysteria 2 YAML 配置
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
            TrafficStatsSecret = node.TrafficStatsSecret != null ? "***encrypted***" : null,
            // Phase 7 新字段
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
    // 3.7 心跳处理（Phase 3 含完整流量扣减 + 幂等检查 + 会话管理 + 超额踢人）
    // ============================

    public async Task ProcessHeartbeatAsync(HeartbeatRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // === 1. 写入节点系统状态（Phase 2 原有） ===
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

            // === 2. 流量处理（Phase 3 新增） ===
            long totalBytesIn = 0, totalBytesOut = 0;
            int activeUsers = 0;

            if (request.UserTraffic != null && request.UserTraffic.Count > 0)
            {
                foreach (var (username, traffic) in request.UserTraffic)
                {
                    // 2a. 生成幂等键
                    var idempotencyKey = TrafficService.GenerateIdempotencyKey(
                        request.NodeId, username, request.ReportedAt);

                    // 2b. 幂等检查
                    var exists = await _trafficRepo.CheckIdempotencyKeyExistsAsync(idempotencyKey);
                    if (exists)
                    {
                        _logger.LogDebug("跳过重复流量: {Key}", idempotencyKey);
                        totalBytesIn += traffic.Rx;
                        totalBytesOut += traffic.Tx;
                        activeUsers++;
                        continue;
                    }

                    // 2c. 获取用户
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        _logger.LogDebug("跳过未知用户流量: {Username}", username);
                        continue;
                    }

                    // 2d. 先更新用户已用流量（乐观并发重试）。
                    //     注意：UpdateUsedTrafficAsync 内部会调用 SaveChangesAsync + ChangeTracker.Clear，
                    //     因此在其之前不能 Add TrafficRecord（否则会被 Clear 丢弃）。
                    await _trafficService.UpdateUsedTrafficAsync(
                        user.Id, traffic.Rx + traffic.Tx);

                    // 2e. 再写入流量记录（UpdateUsedTraffic 已清理 ChangeTracker，此时可安全 Add）
                    _context.TrafficRecords.Add(new TrafficRecord
                    {
                        UserId = user.Id,
                        BytesIn = traffic.Rx,   // Hysteria rx = 用户上传
                        BytesOut = traffic.Tx,  // Hysteria tx = 用户下载
                        NodeId = request.NodeId,
                        IdempotencyKey = idempotencyKey,
                        RecordedAt = request.ReportedAt
                    });

                    // 2f. 检查是否超额 → 触发踢用户下线
                    //     重新加载以获取 UpdateUsedTrafficAsync 后的最新值
                    var updatedUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == user.Id);
                    if (updatedUser != null
                        && updatedUser.TotalTrafficBytes > 0
                        && updatedUser.UsedTrafficBytes >= updatedUser.TotalTrafficBytes)
                    {
                        _logger.LogWarning("用户 {Username} 流量超额: {Used}/{Total}，触发踢下线",
                            username, updatedUser.UsedTrafficBytes, updatedUser.TotalTrafficBytes);

                        // 事务内触发踢人——KickService 使用独立 HttpClient，不影响事务
                        _ = _kickService.KickUserAsync(username, request.NodeId);
                    }

                    totalBytesIn += traffic.Rx;
                    totalBytesOut += traffic.Tx;
                    activeUsers++;
                }
            }

            // === 3. 会话更新（Phase 3 新增） ===
            await _trafficService.UpdateSessionsAsync(request.NodeId, request.OnlineUsers);

            // === 4. 写入节点流量汇总（Phase 3 填充真实值） ===
            _context.NodeTraffics.Add(new NodeTraffic
            {
                NodeId = request.NodeId,
                TotalBytesIn = totalBytesIn,
                TotalBytesOut = totalBytesOut,
                ActiveUsers = activeUsers,
                RecordedAt = DateTime.UtcNow
            });

            // === 5. 更新节点心跳时间和激活状态 ===
            var node = await _nodeRepo.GetByIdAsync(request.NodeId);
            if (node != null)
            {
                node.LastHeartbeat = DateTime.UtcNow;
                node.IsActive = true; // 恢复激活（如果之前离线）
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogDebug("心跳处理完成: NodeId={NodeId}, 流量用户={TrafficCount}, 在线用户={OnlineCount}",
                request.NodeId, activeUsers, request.OnlineUsers?.Count ?? 0);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _logger.LogError("流量扣减并发冲突，事务回滚: NodeId={NodeId}", request.NodeId);
            throw; // 让异常中间件返回 500
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================
    // Phase 7: 管理员更新节点配置
    // ============================

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

    // ============================
    // 3.9 节点密钥轮换
    // ============================

    public async Task<RotateSecretResponse> RotateSecretAsync(string nodeId, long adminId, string clientIp)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        if (node == null)
            throw new NotFoundException("节点不存在");

        // 生成新密钥
        var newSecretPlain = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 256-bit

        // 多版本密钥存储：旧密钥保留在数据库中（通过 SecretVersion 区分）
        // Phase 2 简化实现：直接替换密钥，递增版本号
        // 完整的零停机轮换需要额外的密钥历史表，后续迭代实现
        var oldVersion = node.SecretVersion;
        node.SecretKey = _aes.Encrypt(newSecretPlain);
        node.SecretVersion += 1;

        await _nodeRepo.UpdateAsync(node);

        _logger.LogInformation("节点密钥轮换完成: {NodeId}, 新版本: {Version}", nodeId, node.SecretVersion);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "rotate_secret",
            targetType: "node",
            targetId: nodeId,
            detail: new
            {
                secretVersion = new { from = oldVersion, to = node.SecretVersion }
            },
            clientIp: clientIp
        );

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
    /// Phase 7: 获取节点当前配置版本号（用于心跳响应）。
    /// </summary>
    public async Task<int> GetNodeConfigVersionAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByIdAsync(nodeId);
        return node?.ConfigVersion ?? 1;
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
            ProvisionStatus = node.ProvisionStatus,
            // Phase 7 新字段
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
}
