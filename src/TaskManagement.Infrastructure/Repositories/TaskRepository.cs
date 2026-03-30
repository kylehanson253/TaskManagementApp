using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TaskItem>> GetByTenantAsync(int tenantId) =>
        await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<TaskItem>> GetByTenantAndUserAsync(int tenantId, int userId) =>
        await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Where(t => t.TenantId == tenantId
                     && (t.AssignedToUserId == userId || t.CreatedByUserId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<TaskItem?> GetByIdAndTenantAsync(int id, int tenantId) =>
        await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);

    /// <summary>
    /// Optimized raw SQL query (SQLite equivalent of a stored procedure) that aggregates
    /// task counts by status for a given tenant in a single database round-trip.
    /// </summary>
    public async Task<IEnumerable<TaskSummary>> GetTaskSummaryByTenantAsync(int tenantId)
    {
        var results = await _context.Tasks
            .Where(t => t.TenantId == tenantId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return results.Select(r => new TaskSummary(r.Status, r.Count));
    }
}
