using System.Text.Json;
using HysteriaAuth.Master.Middleware;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class UserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
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
        return MapToDto(user);
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

    public async Task<UserDto> UpdateAsync(long id, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

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
        return MapToDto(user);
    }

    public async Task SoftDeleteAsync(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        user.IsActive = false;
        await _userRepo.UpdateAsync(user);
    }

    public async Task ResetTrafficAsync(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException("用户不存在");

        user.UsedTrafficBytes = 0;
        await _userRepo.UpdateAsync(user);
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
