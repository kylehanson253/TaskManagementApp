using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;

namespace TaskManagement.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(50).IsRequired();
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.Email, u.TenantId }).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();

            e.HasOne(u => u.Tenant)
             .WithMany(t => t.Users)
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskItem
        modelBuilder.Entity<TaskItem>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Priority).HasConversion<string>();

            e.HasOne(t => t.Tenant)
             .WithMany(tn => tn.Tasks)
             .HasForeignKey(t => t.TenantId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.AssignedTo)
             .WithMany(u => u.AssignedTasks)
             .HasForeignKey(t => t.AssignedToUserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.CreatedBy)
             .WithMany(u => u.CreatedTasks)
             .HasForeignKey(t => t.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.Property(rt => rt.Token).HasMaxLength(512).IsRequired();
            e.Ignore(rt => rt.IsExpired);
            e.Ignore(rt => rt.IsRevoked);
            e.Ignore(rt => rt.IsActive);

            e.HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rt => rt.Tenant)
             .WithMany()
             .HasForeignKey(rt => rt.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Tenants
        modelBuilder.Entity<Tenant>().HasData(
            new Tenant { Id = 1, Name = "Acme Corp", Slug = "acme-corp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
            new Tenant { Id = 2, Name = "Tech Startup", Slug = "tech-startup", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true }
        );

        // Users — passwords: Admin@123 and User@123
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1, TenantId = 1, Email = "admin@acme.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FirstName = "Alice", LastName = "Admin",
                Role = UserRole.Admin, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true
            },
            new User
            {
                Id = 2, TenantId = 1, Email = "user@acme.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                FirstName = "Bob", LastName = "User",
                Role = UserRole.User, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true
            },
            new User
            {
                Id = 3, TenantId = 2, Email = "admin@techstartup.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FirstName = "Carol", LastName = "Chief",
                Role = UserRole.Admin, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true
            }
        );

        // Tasks
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem
            {
                Id = 1, TenantId = 1, Title = "Set up CI/CD pipeline",
                Description = "Configure GitHub Actions for automated deployments.",
                Status = TaskItemStatus.InProgress, Priority = TaskPriority.High,
                AssignedToUserId = 2, CreatedByUserId = 1,
                CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem
            {
                Id = 2, TenantId = 1, Title = "Write unit tests for auth module",
                Description = "Cover all authentication and authorization paths.",
                Status = TaskItemStatus.Todo, Priority = TaskPriority.Medium,
                AssignedToUserId = 2, CreatedByUserId = 1,
                CreatedAt = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem
            {
                Id = 3, TenantId = 1, Title = "Review Q1 performance report",
                Status = TaskItemStatus.Completed, Priority = TaskPriority.Low,
                CreatedByUserId = 1,
                CreatedAt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem
            {
                Id = 4, TenantId = 2, Title = "Finalize MVP feature list",
                Description = "Define the scope for the first public release.",
                Status = TaskItemStatus.Todo, Priority = TaskPriority.Critical,
                AssignedToUserId = 3, CreatedByUserId = 3,
                CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
