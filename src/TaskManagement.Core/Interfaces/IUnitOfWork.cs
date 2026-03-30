namespace TaskManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    IUserRepository Users { get; }
    ITenantRepository Tenants { get; }
    Task<int> SaveChangesAsync();
}
