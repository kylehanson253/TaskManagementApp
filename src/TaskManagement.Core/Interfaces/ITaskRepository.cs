using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.Interfaces;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetByTenantAsync(int tenantId);
    Task<IEnumerable<TaskItem>> GetByTenantAndUserAsync(int tenantId, int userId);
    Task<TaskItem?> GetByIdAndTenantAsync(int id, int tenantId);
    Task<IEnumerable<TaskSummary>> GetTaskSummaryByTenantAsync(int tenantId);
}

public record TaskSummary(TaskItemStatus Status, int Count);
