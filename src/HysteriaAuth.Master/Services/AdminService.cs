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

    public AdminService(IAdminRepository adminRepo, JwtService jwtService, IOptions<AdminSettings> options)
    {
        _adminRepo = adminRepo;
        _jwtService = jwtService;
        _settings = options.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
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

    public async Task<AdminDto> CreateAdminAsync(CreateAdminRequest request)
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
        return MapToDto(admin);
    }

    public async Task<List<AdminDto>> GetAllAdminsAsync(int page, int pageSize)
    {
        var (items, _) = await _adminRepo.GetAllAsync(page, pageSize);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AdminDto> UpdateAdminAsync(long adminId, UpdateAdminRequest request)
    {
        var admin = await _adminRepo.GetByIdAsync(adminId);
        if (admin == null)
            throw new NotFoundException("管理员不存在");

        if (request.Role != null) admin.Role = request.Role;
        if (request.IsActive.HasValue) admin.IsActive = request.IsActive.Value;
        if (!string.IsNullOrWhiteSpace(request.Password))
            admin.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

        await _adminRepo.UpdateAsync(admin);
        return MapToDto(admin);
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
