using Microsoft.Extensions.Options;
using HysteriaAuth.Master.Config;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class AdminService
{
    private readonly IAdminRepository _adminRepo;
    private readonly JwtService _jwtService;
    private readonly AdminSettings _settings;
    private readonly AuditService _auditService;

    public AdminService(IAdminRepository adminRepo, JwtService jwtService, IOptions<AdminSettings> options, AuditService auditService)
    {
        _adminRepo = adminRepo;
        _jwtService = jwtService;
        _settings = options.Value;
        _auditService = auditService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp)
    {
        var admin = await _adminRepo.GetByUsernameAsync(request.Username);
        if (admin == null)
            throw new ForbiddenException("invalid_credentials", "管理员用户名或密码错误");

        // 检查是否被锁定
        if (admin.LockedUntil.HasValue && admin.LockedUntil.Value > DateTime.UtcNow)
            throw new ForbiddenException("account_locked", "管理员账号已锁定，请稍后再试");

        // 验证密码
        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.Password))
        {
            // 更新失败次数
            admin.FailedLoginAttempts++;
            if (admin.FailedLoginAttempts >= _settings.MaxFailedLoginAttempts)
                admin.LockedUntil = DateTime.UtcNow.AddMinutes(_settings.LockoutDurationMinutes);

            await _adminRepo.UpdateAsync(admin);

            throw new ForbiddenException("invalid_credentials", "管理员用户名或密码错误");
        }

        // 登录成功：重置失败计数、更新最后登录时间
        admin.FailedLoginAttempts = 0;
        admin.LockedUntil = null;
        admin.LastLoginAt = DateTime.UtcNow;
        await _adminRepo.UpdateAsync(admin);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: admin.Id,
            action: "login",
            targetType: "admin",
            targetId: admin.Id.ToString(),
            detail: new { username = admin.Username },
            clientIp: clientIp
        );

        var token = _jwtService.GenerateToken(admin.Id, admin.Username, admin.Role);
        var expiresAt = _jwtService.GetExpirationTime();

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Admin = new AdminDto
            {
                Id = admin.Id,
                Username = admin.Username,
                Role = admin.Role,
                IsActive = admin.IsActive,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt
            }
        };
    }

    public async Task<AdminDto> CreateAdminAsync(CreateAdminRequest request, long adminId, string clientIp)
    {
        var existing = await _adminRepo.GetByUsernameAsync(request.Username);
        if (existing != null)
            throw new ConflictException("管理员用户名已存在");

        var admin = new Admin
        {
            Username = request.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        admin = await _adminRepo.AddAsync(admin);
        var dto = MapToDto(admin);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: adminId,
            action: "create",
            targetType: "admin",
            targetId: admin.Id.ToString(),
            detail: new
            {
                username = dto.Username,
                dto.Role,
                dto.IsActive
            },
            clientIp: clientIp
        );

        return dto;
    }

    public async Task<List<AdminDto>> GetAllAdminsAsync(int page, int pageSize)
    {
        var (items, _) = await _adminRepo.GetAllAsync(page, pageSize);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AdminDto> UpdateAdminAsync(long adminId, UpdateAdminRequest request, long currentAdminId, string clientIp)
    {
        var admin = await _adminRepo.GetByIdAsync(adminId);
        if (admin == null)
            throw new NotFoundException("管理员不存在");

        // 记录变更前数据
        var before = MapToDto(admin);

        if (request.Role != null) admin.Role = request.Role;
        if (request.IsActive.HasValue) admin.IsActive = request.IsActive.Value;
        if (!string.IsNullOrWhiteSpace(request.Password))
            admin.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

        await _adminRepo.UpdateAsync(admin);

        // 记录变更后数据
        var after = MapToDto(admin);

        // 写入审计日志
        await _auditService.LogAsync(
            adminId: currentAdminId,
            action: "update",
            targetType: "admin",
            targetId: adminId.ToString(),
            detail: new
            {
                before = new { before.Role, before.IsActive },
                after = new { after.Role, after.IsActive },
                changedFields = GetChangedFields(before, after)
            },
            clientIp: clientIp
        );

        return after;
    }

    private static List<string> GetChangedFields(AdminDto before, AdminDto after)
    {
        var changedFields = new List<string>();
        if (before.Role != after.Role) changedFields.Add("Role");
        if (before.IsActive != after.IsActive) changedFields.Add("IsActive");
        return changedFields;
    }

    private static AdminDto MapToDto(Admin admin)
    {
        return new AdminDto
        {
            Id = admin.Id,
            Username = admin.Username,
            Role = admin.Role,
            IsActive = admin.IsActive,
            CreatedAt = admin.CreatedAt,
            LastLoginAt = admin.LastLoginAt
        };
    }
}
