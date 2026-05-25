using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AdminAuditLog log);
    Task<(List<AdminAuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query);
}
