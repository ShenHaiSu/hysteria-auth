using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Data;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly AppDbContext _context;

    public NodeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Node?> GetByIdAsync(string nodeId)
    {
        return await _context.Nodes.FindAsync(nodeId);
    }

    public async Task<Node?> GetByProvisionTokenAsync(string token)
    {
        return await _context.Nodes
            .FirstOrDefaultAsync(n => n.ProvisionToken == token);
    }

    public async Task<(List<Node> Items, int Total)> GetAllAsync(int page, int pageSize, bool? isActive = null, string? provisionStatus = null)
    {
        var query = _context.Nodes.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(n => n.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(provisionStatus))
            query = query.Where(n => n.ProvisionStatus == provisionStatus);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Node>> GetActiveNodesAsync()
    {
        return await _context.Nodes
            .Where(n => n.IsActive)
            .ToListAsync();
    }

    public async Task<Node> AddAsync(Node node)
    {
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();
        return node;
    }

    public async Task<Node> UpdateAsync(Node node)
    {
        _context.Nodes.Update(node);
        await _context.SaveChangesAsync();
        return node;
    }
}
