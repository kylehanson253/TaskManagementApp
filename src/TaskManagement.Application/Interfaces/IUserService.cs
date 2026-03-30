using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync(int tenantId);
    Task<UserDto> GetUserByIdAsync(int id, int tenantId);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, int tenantId);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, int tenantId);
    Task DeleteUserAsync(int id, int tenantId);
}
