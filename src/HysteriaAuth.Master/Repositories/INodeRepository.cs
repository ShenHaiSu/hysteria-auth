using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface INodeRepository
{
    Task<Node?> GetByIdAsync(string nodeId);
    Task<Node?> GetByProvisionTokenAsync(string token);
    Task<(List<Node> Items, int Total)> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<List<Node>> GetActiveNodesAsync();
    Task<Node> AddAsync(Node node);
    Task<Node> UpdateAsync(Node node);
}
