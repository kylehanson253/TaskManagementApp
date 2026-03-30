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

        return await BuildAuthResponseAsync(user);
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
        var stored = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (!stored.IsActive)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        var user = await _unitOfWork.Users.GetByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Rotate: revoke the used token and issue a new one
        stored.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.RefreshTokens.UpdateAsync(stored);
        await _unitOfWork.SaveChangesAsync();

        return await BuildAuthResponseAsync(user);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken);
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var tokenEntity = new RefreshToken
        {
            Token = rawRefreshToken,
            UserId = user.Id,
            TenantId = user.TenantId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _unitOfWork.RefreshTokens.AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = rawRefreshToken,
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
