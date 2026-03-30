using TaskManagement.Core.Entities;

namespace TaskManagement.Core.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeTokenAsync(string token);
    Task RevokeAllUserTokensAsync(int userId);
}
