using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Extensions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>Get all tasks for the caller's tenant. Admins see all; Users see only their own.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks()
    {
        var tasks = await _taskService.GetTasksAsync(
            User.GetTenantId(), User.GetUserId(), User.IsAdmin());
        return Ok(tasks);
    }

    /// <summary>Get a single task by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> GetTask(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(
            id, User.GetTenantId(), User.GetUserId(), User.IsAdmin());
        return Ok(task);
    }

    /// <summary>Get a summary of task counts by status for the tenant's dashboard.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<TaskSummaryDto>> GetSummary()
    {
        var summary = await _taskService.GetTaskSummaryAsync(User.GetTenantId());
        return Ok(summary);
    }

    /// <summary>Create a new task in the caller's tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.CreateTaskAsync(dto, User.GetTenantId(), User.GetUserId());
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    /// <summary>Update a task. Admins can update any task; Users can only update their own.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskDto>> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.UpdateTaskAsync(
            id, dto, User.GetTenantId(), User.GetUserId(), User.IsAdmin());
        return Ok(task);
    }

    /// <summary>Mark a task as complete.</summary>
    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<TaskDto>> CompleteTask(int id)
    {
        var task = await _taskService.CompleteTaskAsync(
            id, User.GetTenantId(), User.GetUserId(), User.IsAdmin());
        return Ok(task);
    }

    /// <summary>Delete a task. Admins can delete any task; Users can only delete their own.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        await _taskService.DeleteTaskAsync(
            id, User.GetTenantId(), User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}
