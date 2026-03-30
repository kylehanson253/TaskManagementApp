using TaskManagement.Core.Entities;

namespace TaskManagement.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailAndTenantAsync(string email, int tenantId);
    Task<IEnumerable<User>> GetByTenantAsync(int tenantId);
    Task<User?> GetByIdAndTenantAsync(int id, int tenantId);
}
