using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByUsernameAsync(string username);
    Task<(List<User> Items, int Total)> GetAllAsync(int page, int pageSize, string? search, bool? isActive, string? nodeId);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> CheckUsernameExistsAsync(string username);
}
