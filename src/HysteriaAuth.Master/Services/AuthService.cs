using System.Text.Json;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class AuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthLogRepository _authLogRepo;

    public AuthService(IUserRepository userRepo, IAuthLogRepository authLogRepo)
    {
        _userRepo = userRepo;
        _authLogRepo = authLogRepo;
    }

    /// <summary>
    /// 执行 Hysteria 用户认证，严格按 6 步顺序检查，顺序不可变。
    /// </summary>
    public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
    {
        // Step 1: 用户存在?
        var user = await _userRepo.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            await LogFailureAsync(null, request, "invalid_credentials");
            throw new ForbiddenException("invalid_credentials", "用户名或密码错误");
        }

        // Step 2: 密码正确?
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            await LogFailureAsync(user.Id, request, "invalid_credentials");
            throw new ForbiddenException("invalid_credentials", "用户名或密码错误");
        }

        // Step 3: 账号激活?
        if (!user.IsActive)
        {
            await LogFailureAsync(user.Id, request, "account_disabled");
            throw new ForbiddenException("account_disabled", "账号已禁用");
        }

        // Step 4: 账号过期?
        if (user.ExpiresAt.HasValue && user.ExpiresAt.Value < DateTime.UtcNow)
        {
            await LogFailureAsync(user.Id, request, "account_expired");
            throw new ForbiddenException("account_expired", "账号已过期");
        }

        // Step 5: 流量充足?
        if (user.UsedTrafficBytes >= user.TotalTrafficBytes)
        {
            await LogFailureAsync(user.Id, request, "traffic_exhausted");
            throw new ForbiddenException("traffic_exhausted", "流量已用尽");
        }

        // Step 6: 节点允许?
        if (!string.IsNullOrEmpty(user.AllowedNodes))
        {
            try
            {
                var allowedNodes = JsonSerializer.Deserialize<List<string>>(user.AllowedNodes);
                if (allowedNodes != null && allowedNodes.Count > 0 && !allowedNodes.Contains(request.NodeId))
                {
                    await LogFailureAsync(user.Id, request, "node_not_allowed");
                    throw new ForbiddenException("node_not_allowed", "不允许使用该节点");
                }
            }
            catch (JsonException)
            {
                // JSON 解析失败视为无限制
            }
        }

        // 认证成功 → 记录日志
        await _authLogRepo.AddAsync(new AuthLog
        {
            UserId = user.Id,
            Username = user.Username,
            NodeId = request.NodeId,
            ClientIp = request.ClientIp,
            Success = true,
            AuthTime = DateTime.UtcNow
        });

        var remainingTraffic = user.TotalTrafficBytes - user.UsedTrafficBytes;
        if (remainingTraffic < 0) remainingTraffic = 0;

        return new AuthResponse
        {
            Success = true,
            UserId = user.Id,
            Message = "Authentication successful",
            RemainingTraffic = remainingTraffic,
            ExpiresAt = user.ExpiresAt
        };
    }

    private async Task LogFailureAsync(long? userId, AuthRequest request, string reason)
    {
        await _authLogRepo.AddAsync(new AuthLog
        {
            UserId = userId,
            Username = request.Username,
            NodeId = request.NodeId,
            ClientIp = request.ClientIp,
            Success = false,
            Reason = reason,
            AuthTime = DateTime.UtcNow
        });
    }
}
