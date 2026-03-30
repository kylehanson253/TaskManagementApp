using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Tests.Helpers;

namespace TaskManagement.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly UnitOfWork _unitOfWork;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var dbName = $"AuthServiceTests_{Guid.NewGuid()}";
        var ctx = DbContextFactory.CreateSeeded(dbName);
        _unitOfWork = new UnitOfWork(ctx);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "Test-Secret-Key-For-Unit-Tests-MinLength32!!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            })
            .Build();

        var jwtService = new JwtService(config);
        _authService = new AuthService(_unitOfWork, jwtService, new Mock<ILogger<AuthService>>().Object);
    }

    // ── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var dto = new LoginDto { Email = "admin@test.com", Password = "Admin@123" };

        var result = await _authService.LoginAsync(dto);

        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var dto = new LoginDto { Email = "admin@test.com", Password = "WrongPassword" };

        var act = async () => await _authService.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ThrowsUnauthorized()
    {
        var dto = new LoginDto { Email = "nobody@test.com", Password = "anything" };

        var act = async () => await _authService.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesUser()
    {
        var dto = new RegisterDto
        {
            Email = "newuser@test.com",
            Password = "NewUser@123",
            FirstName = "New",
            LastName = "User",
            TenantSlug = "test-corp"
        };

        var result = await _authService.RegisterAsync(dto);

        result.Email.Should().Be("newuser@test.com");
        result.TenantId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        var dto = new RegisterDto
        {
            Email = "admin@test.com",  // already exists
            Password = "Admin@123",
            FirstName = "Dup",
            LastName = "User",
            TenantSlug = "test-corp"
        };

        var act = async () => await _authService.RegisterAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegisterAsync_InvalidTenantSlug_ThrowsKeyNotFound()
    {
        var dto = new RegisterDto
        {
            Email = "new@test.com",
            Password = "Pass@123",
            FirstName = "New",
            LastName = "User",
            TenantSlug = "nonexistent-slug"
        };

        var act = async () => await _authService.RegisterAsync(dto);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Refresh ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        var loginResult = await _authService.LoginAsync(
            new LoginDto { Email = "admin@test.com", Password = "Admin@123" });

        var refreshResult = await _authService.RefreshTokenAsync(loginResult.RefreshToken);

        refreshResult.Token.Should().NotBeNullOrEmpty();
        refreshResult.Token.Should().NotBe(loginResult.Token);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorized()
    {
        var act = async () => await _authService.RefreshTokenAsync("not-a-valid-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _unitOfWork.Dispose();
}
