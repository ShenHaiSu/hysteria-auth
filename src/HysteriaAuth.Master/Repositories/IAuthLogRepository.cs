using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Repositories;

public interface IAuthLogRepository
{
    Task AddAsync(AuthLog log);
}
