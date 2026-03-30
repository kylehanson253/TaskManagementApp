using TaskManagement.Core.Entities;

namespace TaskManagement.Core.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug);
}
