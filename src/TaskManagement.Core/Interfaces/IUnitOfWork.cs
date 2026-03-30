namespace TaskManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    IUserRepository Users { get; }
    ITenantRepository Tenants { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    Task<int> SaveChangesAsync();
}
