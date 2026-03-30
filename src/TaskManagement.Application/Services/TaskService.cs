using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskService> _logger;

    public TaskService(IUnitOfWork unitOfWork, ILogger<TaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync(int tenantId, int requestingUserId, bool isAdmin)
    {
        var tasks = isAdmin
            ? await _unitOfWork.Tasks.GetByTenantAsync(tenantId)
            : await _unitOfWork.Tasks.GetByTenantAndUserAsync(tenantId, requestingUserId);

        return tasks.Select(MapToDto);
    }

    public async Task<TaskDto> GetTaskByIdAsync(int id, int tenantId, int requestingUserId, bool isAdmin)
    {
        var task = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!isAdmin && task.AssignedToUserId != requestingUserId && task.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("You do not have access to this task.");

        return MapToDto(task);
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, int tenantId, int createdByUserId)
    {
        if (dto.AssignedToUserId.HasValue)
        {
            var assignee = await _unitOfWork.Users.GetByIdAndTenantAsync(dto.AssignedToUserId.Value, tenantId);
            if (assignee == null)
                throw new KeyNotFoundException("Assigned user not found in this tenant.");
        }

        var task = new TaskItem
        {
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedByUserId = createdByUserId,
            DueDate = dto.DueDate?.ToUniversalTime(),
            Status = TaskItemStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task '{Title}' created in tenant {TenantId} by user {UserId}.", task.Title, tenantId, createdByUserId);

        var created = await _unitOfWork.Tasks.GetByIdAndTenantAsync(task.Id, tenantId);
        return MapToDto(created!);
    }

    public async Task<TaskDto> UpdateTaskAsync(int id, UpdateTaskDto dto, int tenantId, int requestingUserId, bool isAdmin)
    {
        var task = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!isAdmin && task.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Only the task creator or an admin can edit this task.");

        if (dto.AssignedToUserId.HasValue)
        {
            var assignee = await _unitOfWork.Users.GetByIdAndTenantAsync(dto.AssignedToUserId.Value, tenantId);
            if (assignee == null)
                throw new KeyNotFoundException("Assigned user not found in this tenant.");
        }

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.AssignedToUserId = dto.AssignedToUserId;
        task.DueDate = dto.DueDate?.ToUniversalTime();
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task {Id} updated in tenant {TenantId}.", id, tenantId);

        var updated = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId);
        return MapToDto(updated!);
    }

    public async Task DeleteTaskAsync(int id, int tenantId, int requestingUserId, bool isAdmin)
    {
        var task = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!isAdmin && task.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Only the task creator or an admin can delete this task.");

        await _unitOfWork.Tasks.DeleteAsync(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task {Id} deleted from tenant {TenantId}.", id, tenantId);
    }

    public async Task<TaskDto> CompleteTaskAsync(int id, int tenantId, int requestingUserId, bool isAdmin)
    {
        var task = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!isAdmin && task.AssignedToUserId != requestingUserId && task.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("You do not have permission to complete this task.");

        task.Status = TaskItemStatus.Completed;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task {Id} marked complete in tenant {TenantId}.", id, tenantId);

        var completed = await _unitOfWork.Tasks.GetByIdAndTenantAsync(id, tenantId);
        return MapToDto(completed!);
    }

    public async Task<TaskSummaryDto> GetTaskSummaryAsync(int tenantId)
    {
        var summaries = await _unitOfWork.Tasks.GetTaskSummaryByTenantAsync(tenantId);
        var dict = summaries.ToDictionary(s => s.Status, s => s.Count);

        return new TaskSummaryDto
        {
            TodoCount = dict.GetValueOrDefault(TaskItemStatus.Todo),
            InProgressCount = dict.GetValueOrDefault(TaskItemStatus.InProgress),
            CompletedCount = dict.GetValueOrDefault(TaskItemStatus.Completed),
            CancelledCount = dict.GetValueOrDefault(TaskItemStatus.Cancelled),
            TotalTasks = dict.Values.Sum()
        };
    }

    private static TaskDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        AssignedToUserId = task.AssignedToUserId,
        AssignedToName = task.AssignedTo != null
            ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}"
            : null,
        CreatedByUserId = task.CreatedByUserId,
        CreatedByName = task.CreatedBy != null
            ? $"{task.CreatedBy.FirstName} {task.CreatedBy.LastName}"
            : string.Empty,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        DueDate = task.DueDate,
        TenantId = task.TenantId
    };
}
