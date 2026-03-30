using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    // In-memory refresh token store — in production use a DB-backed table.
    private static readonly Dictionary<string, (int UserId, int TenantId, DateTime Expires)> _refreshTokens = new();

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        _logger.LogInformation("User {Email} (Tenant {TenantId}) logged in successfully.", user.Email, user.TenantId);

        return BuildAuthResponse(user);
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        var tenant = await _unitOfWork.Tenants.GetBySlugAsync(dto.TenantSlug)
            ?? throw new KeyNotFoundException($"Tenant '{dto.TenantSlug}' not found.");

        var existing = await _unitOfWork.Users.GetByEmailAndTenantAsync(dto.Email, tenant.Id);
        if (existing != null)
            throw new InvalidOperationException("A user with this email already exists in this tenant.");

        var user = new User
        {
            TenantId = tenant.Id,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("New user {Email} registered in tenant {Slug}.", user.Email, tenant.Slug);

        return MapToUserDto(user, tenant.Name);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var tokenData))
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (tokenData.Expires < DateTime.UtcNow)
        {
            _refreshTokens.Remove(refreshToken);
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(tokenData.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        _refreshTokens.Remove(refreshToken);
        return BuildAuthResponse(user);
    }

    public Task RevokeTokenAsync(string refreshToken)
    {
        _refreshTokens.Remove(refreshToken);
        return Task.CompletedTask;
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _refreshTokens[refreshToken] = (user.Id, user.TenantId, DateTime.UtcNow.AddDays(7));

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user, user.Tenant?.Name ?? string.Empty)
        };
    }

    private static UserDto MapToUserDto(User user, string tenantName) => new()
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
