using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Core.Enums;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Tests.Helpers;

namespace TaskManagement.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly UnitOfWork _unitOfWork;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        var dbName = $"TaskServiceTests_{Guid.NewGuid()}";
        var ctx = DbContextFactory.CreateSeeded(dbName);
        _unitOfWork = new UnitOfWork(ctx);
        _taskService = new TaskService(_unitOfWork, new Mock<ILogger<TaskService>>().Object);
    }

    // ── GetTasks ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasksAsync_Admin_ReturnsAllTenantTasks()
    {
        var tasks = await _taskService.GetTasksAsync(tenantId: 1, requestingUserId: 1, isAdmin: true);

        tasks.Should().HaveCount(2);
        tasks.Should().OnlyContain(t => t.TenantId == 1);
    }

    [Fact]
    public async Task GetTasksAsync_User_ReturnsOnlyOwnTasks()
    {
        // User 2 is assigned task 1 and created task 2 — both in tenant 1
        var tasks = await _taskService.GetTasksAsync(tenantId: 1, requestingUserId: 2, isAdmin: false);

        tasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTasksAsync_DoesNotLeakCrossTenantData()
    {
        // Tenant 2 tasks should never appear for tenant 1 requests
        var tasks = await _taskService.GetTasksAsync(tenantId: 1, requestingUserId: 1, isAdmin: true);

        tasks.Should().NotContain(t => t.TenantId == 2);
    }

    // ── CreateTask ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTaskAsync_ValidDto_CreatesAndReturnsTask()
    {
        var dto = new CreateTaskDto
        {
            Title = "New Task",
            Description = "Test description",
            Priority = TaskPriority.High,
            AssignedToUserId = 2
        };

        var result = await _taskService.CreateTaskAsync(dto, tenantId: 1, createdByUserId: 1);

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Task");
        result.Status.Should().Be(TaskItemStatus.Todo);
        result.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task CreateTaskAsync_AssigneeFromDifferentTenant_ThrowsKeyNotFoundException()
    {
        var dto = new CreateTaskDto
        {
            Title = "Bad Assign",
            AssignedToUserId = 3  // User 3 belongs to tenant 2
        };

        var act = async () => await _taskService.CreateTaskAsync(dto, tenantId: 1, createdByUserId: 1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found in this tenant*");
    }

    // ── CompleteTask ────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteTaskAsync_TaskCreator_CanCompleteTask()
    {
        var result = await _taskService.CompleteTaskAsync(
            id: 1, tenantId: 1, requestingUserId: 1, isAdmin: false);

        result.Status.Should().Be(TaskItemStatus.Completed);
    }

    [Fact]
    public async Task CompleteTaskAsync_UnrelatedUser_ThrowsUnauthorized()
    {
        // User 2 didn't create or get assigned task... wait, they are assigned to task 1.
        // Let's use admin user (1) on a task with a different user who has no relation
        // Actually seed task 2: created by user 2, no assignment.
        // Try as user 1 (not admin, not creator, not assignee)
        var act = async () => await _taskService.CompleteTaskAsync(
            id: 2, tenantId: 1, requestingUserId: 1, isAdmin: false);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CompleteTaskAsync_Admin_CanCompleteAnyTask()
    {
        var result = await _taskService.CompleteTaskAsync(
            id: 2, tenantId: 1, requestingUserId: 1, isAdmin: true);

        result.Status.Should().Be(TaskItemStatus.Completed);
    }

    // ── DeleteTask ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTaskAsync_TaskNotInTenant_ThrowsKeyNotFoundException()
    {
        // Task 3 belongs to tenant 2 — tenant 1 users cannot delete it
        var act = async () => await _taskService.DeleteTaskAsync(
            id: 3, tenantId: 1, requestingUserId: 1, isAdmin: true);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetTaskSummary ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTaskSummaryAsync_ReturnCorrectCounts()
    {
        var summary = await _taskService.GetTaskSummaryAsync(tenantId: 1);

        summary.TotalTasks.Should().Be(2);
        summary.TodoCount.Should().Be(1);
        summary.CompletedCount.Should().Be(1);
        summary.InProgressCount.Should().Be(0);
    }

    public void Dispose() => _unitOfWork.Dispose();
}
