using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public class AuthLogRepository : IAuthLogRepository
{
    private readonly AppDbContext _context;

    public AuthLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuthLog log)
    {
        _context.AuthLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
