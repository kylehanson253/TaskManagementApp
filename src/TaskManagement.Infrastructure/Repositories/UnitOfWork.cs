using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private TaskRepository? _taskRepository;
    private UserRepository? _userRepository;
    private TenantRepository? _tenantRepository;
    private RefreshTokenRepository? _refreshTokenRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ITaskRepository Tasks =>
        _taskRepository ??= new TaskRepository(_context);

    public IUserRepository Users =>
        _userRepository ??= new UserRepository(_context);

    public ITenantRepository Tenants =>
        _tenantRepository ??= new TenantRepository(_context);

    public IRefreshTokenRepository RefreshTokens =>
        _refreshTokenRepository ??= new RefreshTokenRepository(_context);

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public void Dispose() =>
        _context.Dispose();
}
