using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface IAdminRepository
{
    Task<Admin?> GetByIdAsync(long id);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<(List<Admin> Items, int Total)> GetAllAsync(int page, int pageSize);
    Task<Admin> AddAsync(Admin admin);
    Task UpdateAsync(Admin admin);
}
