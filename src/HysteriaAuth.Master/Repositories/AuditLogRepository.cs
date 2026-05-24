using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAuditLog log)
    {
        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AdminAuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query)
    {
        var q = _context.AdminAuditLogs
            .Include(a => a.Admin)
            .AsQueryable();

        if (query.AdminId.HasValue)
            q = q.Where(l => l.AdminId == query.AdminId.Value);
        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(l => l.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.TargetType))
            q = q.Where(l => l.TargetType == query.TargetType);
        if (query.StartTime.HasValue)
            q = q.Where(l => l.CreatedAt >= query.StartTime.Value);
        if (query.EndTime.HasValue)
            q = q.Where(l => l.CreatedAt < query.EndTime.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, total);
    }
}
