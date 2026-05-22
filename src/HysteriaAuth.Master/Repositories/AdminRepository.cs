using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> GetByIdAsync(long id)
    {
        return await _context.Admins.FindAsync(id);
    }

    public async Task<Admin?> GetByUsernameAsync(string username)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<(List<Admin> Items, int Total)> GetAllAsync(int page, int pageSize)
    {
        var total = await _context.Admins.CountAsync();
        var items = await _context.Admins
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Admin> AddAsync(Admin admin)
    {
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();
        return admin;
    }

    public async Task UpdateAsync(Admin admin)
    {
        _context.Admins.Update(admin);
        await _context.SaveChangesAsync();
    }
}
