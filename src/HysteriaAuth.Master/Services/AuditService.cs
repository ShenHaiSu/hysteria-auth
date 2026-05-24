using System.Text.Json;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditLogRepository auditLogRepo,
        ILogger<AuditService> logger)
    {
        _auditLogRepo = auditLogRepo;
        _logger = logger;
    }

    /// <summary>
    /// 记录审计日志。
    /// </summary>
    /// <param name="adminId">操作管理员 ID</param>
    /// <param name="action">操作类型：create/update/delete/login/kick_user/rotate_secret</param>
    /// <param name="targetType">目标类型：user/node/admin/system</param>
    /// <param name="targetId">目标 ID（字符串）</param>
    /// <param name="detail">操作详情对象（将被序列化为 JSON 字符串）</param>
    /// <param name="clientIp">操作来源 IP</param>
    public async Task LogAsync(
        long adminId,
        string action,
        string targetType,
        string? targetId,
        object? detail,
        string clientIp)
    {
        var log = new AdminAuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepo.AddAsync(log);

        _logger.LogInformation(
            "审计日志: AdminId={AdminId} Action={Action} Target={TargetType}:{TargetId} IP={ClientIp}",
            adminId, action, targetType, targetId, clientIp);
    }

    /// <summary>
    /// 查询审计日志（分页 + 多维筛选）。
    /// </summary>
    public async Task<AuditLogListResponse> GetPagedAsync(AuditLogQuery query)
    {
        // pageSize 最大限制 200
        if (query.PageSize > 200) query.PageSize = 200;

        var (items, total) = await _auditLogRepo.GetPagedAsync(query);

        return new AuditLogListResponse
        {
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    private static AuditLogDto MapToDto(AdminAuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            AdminId = log.AdminId,
            AdminName = log.Admin?.Username ?? "unknown",
            Action = log.Action,
            TargetType = log.TargetType,
            TargetId = log.TargetId,
            Detail = log.Detail,
            ClientIp = log.ClientIp,
            CreatedAt = log.CreatedAt
        };
    }
}
