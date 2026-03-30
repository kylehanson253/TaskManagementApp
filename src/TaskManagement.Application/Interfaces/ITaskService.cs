using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetTasksAsync(int tenantId, int requestingUserId, bool isAdmin);
    Task<TaskDto> GetTaskByIdAsync(int id, int tenantId, int requestingUserId, bool isAdmin);
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, int tenantId, int createdByUserId);
    Task<TaskDto> UpdateTaskAsync(int id, UpdateTaskDto dto, int tenantId, int requestingUserId, bool isAdmin);
    Task DeleteTaskAsync(int id, int tenantId, int requestingUserId, bool isAdmin);
    Task<TaskDto> CompleteTaskAsync(int id, int tenantId, int requestingUserId, bool isAdmin);
    Task<TaskSummaryDto> GetTaskSummaryAsync(int tenantId);
}
