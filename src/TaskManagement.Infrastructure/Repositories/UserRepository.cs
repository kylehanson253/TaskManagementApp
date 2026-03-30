using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<User?> GetByEmailAndTenantAsync(string email, int tenantId) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.TenantId == tenantId);

    public async Task<IEnumerable<User>> GetByTenantAsync(int tenantId) =>
        await _context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync();

    public async Task<User?> GetByIdAndTenantAsync(int id, int tenantId) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
}
