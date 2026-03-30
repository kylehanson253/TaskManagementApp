using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Tests.Helpers;

public static class DbContextFactory
{
    public static ApplicationDbContext CreateInMemory(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    public static ApplicationDbContext CreateSeeded(string dbName)
    {
        var ctx = CreateInMemory(dbName);

        ctx.Tenants.AddRange(
            new Tenant { Id = 1, Name = "Test Corp", Slug = "test-corp", CreatedAt = DateTime.UtcNow, IsActive = true },
            new Tenant { Id = 2, Name = "Other Corp", Slug = "other-corp", CreatedAt = DateTime.UtcNow, IsActive = true }
        );

        ctx.Users.AddRange(
            new User
            {
                Id = 1, TenantId = 1, Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FirstName = "Test", LastName = "Admin",
                Role = UserRole.Admin, CreatedAt = DateTime.UtcNow, IsActive = true
            },
            new User
            {
                Id = 2, TenantId = 1, Email = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                FirstName = "Test", LastName = "User",
                Role = UserRole.User, CreatedAt = DateTime.UtcNow, IsActive = true
            },
            new User
            {
                Id = 3, TenantId = 2, Email = "other@other.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Other@123"),
                FirstName = "Other", LastName = "User",
                Role = UserRole.User, CreatedAt = DateTime.UtcNow, IsActive = true
            }
        );

        ctx.Tasks.AddRange(
            new TaskItem
            {
                Id = 1, TenantId = 1, Title = "Task One",
                Status = TaskItemStatus.Todo, Priority = TaskPriority.Medium,
                CreatedByUserId = 1, AssignedToUserId = 2,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = 2, TenantId = 1, Title = "Task Two",
                Status = TaskItemStatus.Completed, Priority = TaskPriority.Low,
                CreatedByUserId = 2,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = 3, TenantId = 2, Title = "Other Tenant Task",
                Status = TaskItemStatus.Todo, Priority = TaskPriority.High,
                CreatedByUserId = 3,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }
        );

        ctx.SaveChanges();
        return ctx;
    }
}
