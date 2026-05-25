using System.Text.Json;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class UserService
{
    private readonly IUserRepository _userRepo;
    private readonly AuditService _auditService;

    public UserService(IUserRepository userRepo, AuditService auditService)
    {
        _userRepo = userRepo;
        _auditService = auditService;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, long adminId, string clientIp)
    {
        if (await _userRepo.CheckUsernameExistsAsync(request.Username))
            throw new ConflictException("用户名已存在");

        var user = new User
        {
            Username = request.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Email = request.Email,
            TotalTrafficBytes = request.TotalTrafficBytes,
            IsActive = request.IsActive,
            ExpiresAt = request.ExpiresAt,
            AllowedNodes = request.AllowedNodes != null
                ? JsonSerializer.Serialize(request.AllowedNodes)
                : null,
            Remark = request.Remark,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user = await _userRepo.AddAsync(user);
        var dto = MapToDto(user);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "create",
            targetType: "user",
            targetId: user.Id.ToString(),
            detail: new
            {
                username = dto.Username,
                dto.Email,
                dto.TotalTrafficBytes,
                dto.IsActive,
                dto.Remark
            },
            clientIp: clientIp
        );

        return dto;
    }

    public async Task<UserListResponse> GetAllAsync(int page, int pageSize, string? search, bool? isActive, string? nodeId)
    {
        var (items, total) = await _userRepo.GetAllAsync(page, pageSize, search, isActive, nodeId);

        return new UserListResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    public async Task<UserDto> GetByIdAsync(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(long id, UpdateUserRequest request, long adminId, string clientIp)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        // 记录变更前数据
        var before = MapToDto(user);

        if (request.Email != null) user.Email = request.Email;
        if (request.TotalTrafficBytes.HasValue) user.TotalTrafficBytes = request.TotalTrafficBytes.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.ExpiresAt != null) user.ExpiresAt = request.ExpiresAt;
        if (request.AllowedNodes != null)
            user.AllowedNodes = JsonSerializer.Serialize(request.AllowedNodes);
        if (request.Remark != null) user.Remark = request.Remark;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

        await _userRepo.UpdateAsync(user);

        // 记录变更后数据
        var after = MapToDto(user);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "update",
            targetType: "user",
            targetId: id.ToString(),
            detail: new
            {
                before = new
                {
                    before.Email,
                    before.IsActive,
                    before.TotalTrafficBytes,
                    before.UsedTrafficBytes,
                    before.Remark
                },
                after = new
                {
                    after.Email,
                    after.IsActive,
                    after.TotalTrafficBytes,
                    after.UsedTrafficBytes,
                    after.Remark
                },
                changedFields = GetChangedFields(before, after)
            },
            clientIp: clientIp
        );

        return after;
    }

    public async Task SoftDeleteAsync(long id, long adminId, string clientIp)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        // 记录变更前数据
        var before = MapToDto(user);

        user.IsActive = false;
        await _userRepo.UpdateAsync(user);

        // 记录变更后数据
        var after = MapToDto(user);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "delete",
            targetType: "user",
            targetId: id.ToString(),
            detail: new
            {
                before = new { before.IsActive },
                after = new { after.IsActive }
            },
            clientIp: clientIp
        );
    }

    public async Task ResetTrafficAsync(long id, long adminId, string clientIp)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        // 记录变更前数据
        var beforeUsedTraffic = user.UsedTrafficBytes;

        user.UsedTrafficBytes = 0;
        await _userRepo.UpdateAsync(user);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "update",
            targetType: "user",
            targetId: id.ToString(),
            detail: new
            {
                before = new { usedTrafficBytes = beforeUsedTraffic },
                after = new { usedTrafficBytes = 0 }
            },
            clientIp: clientIp
        );
    }

    private static List<string> GetChangedFields(UserDto before, UserDto after)
    {
        var changedFields = new List<string>();
        if (before.Email != after.Email) changedFields.Add("Email");
        if (before.IsActive != after.IsActive) changedFields.Add("IsActive");
        if (before.TotalTrafficBytes != after.TotalTrafficBytes) changedFields.Add("TotalTrafficBytes");
        if (before.UsedTrafficBytes != after.UsedTrafficBytes) changedFields.Add("UsedTrafficBytes");
        if (before.Remark != after.Remark) changedFields.Add("Remark");
        return changedFields;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            TotalTrafficBytes = user.TotalTrafficBytes,
            UsedTrafficBytes = user.UsedTrafficBytes,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ExpiresAt = user.ExpiresAt,
            AllowedNodes = user.AllowedNodes,
            Remark = user.Remark
        };
    }
}
