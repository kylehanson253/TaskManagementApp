using TaskManagement.Core.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    (int userId, int tenantId, string role) ValidateRefreshToken(string token);
}
