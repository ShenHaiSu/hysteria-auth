using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface INodeStatusRepository
{
    Task AddAsync(NodeStatus status);
    Task<List<NodeStatus>> GetHistoryAsync(string nodeId, int hours);
}
