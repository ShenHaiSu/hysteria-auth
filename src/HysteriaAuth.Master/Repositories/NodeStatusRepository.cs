using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public class NodeStatusRepository : INodeStatusRepository
{
    private readonly AppDbContext _context;

    public NodeStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NodeStatus status)
    {
        _context.NodeStatuses.Add(status);
        await _context.SaveChangesAsync();
    }

    public async Task<List<NodeStatus>> GetHistoryAsync(string nodeId, int hours)
    {
        var since = DateTime.UtcNow.AddHours(-hours);
        return await _context.NodeStatuses
            .Where(ns => ns.NodeId == nodeId && ns.ReportedAt >= since)
            .OrderByDescending(ns => ns.ReportedAt)
            .ToListAsync();
    }
}
