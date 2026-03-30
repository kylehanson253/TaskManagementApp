using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(int tenantId)
    {
        var users = await _unitOfWork.Users.GetByTenantAsync(tenantId);
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
        return users.Select(u => MapToDto(u, tenant?.Name ?? string.Empty));
    }

    public async Task<UserDto> GetUserByIdAsync(int id, int tenantId)
    {
        var user = await _unitOfWork.Users.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"User {id} not found in this tenant.");
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
        return MapToDto(user, tenant?.Name ?? string.Empty);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, int tenantId)
    {
        var existing = await _unitOfWork.Users.GetByEmailAndTenantAsync(dto.Email, tenantId);
        if (existing != null)
            throw new InvalidOperationException("A user with this email already exists in this tenant.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId)
            ?? throw new KeyNotFoundException("Tenant not found.");

        var user = new User
        {
            TenantId = tenantId,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Admin created user {Email} in tenant {TenantId}.", user.Email, tenantId);
        return MapToDto(user, tenant.Name);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, int tenantId)
    {
        var user = await _unitOfWork.Users.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user, tenant?.Name ?? string.Empty);
    }

    public async Task DeleteUserAsync(int id, int tenantId)
    {
        var user = await _unitOfWork.Users.GetByIdAndTenantAsync(id, tenantId)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Id} deleted from tenant {TenantId}.", id, tenantId);
    }

    private static UserDto MapToDto(User user, string tenantName) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        TenantId = user.TenantId,
        TenantName = tenantName,
        IsActive = user.IsActive
    };
}
