using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Authenticate and receive a JWT access token. Refresh token is set as an httpOnly cookie.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.LoginAsync(dto);
            SetRefreshTokenCookie(result.RefreshToken);
            result.RefreshToken = string.Empty;
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Failed login attempt for {Email}: {Message}", dto.Email, ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Register a new user within an existing tenant.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _authService.RegisterAsync(dto);
        return CreatedAtAction(nameof(Login), user);
    }

    /// <summary>Silently renew the access token using the httpOnly refresh token cookie.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "No refresh token provided." });

        try
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            SetRefreshTokenCookie(result.RefreshToken);
            result.RefreshToken = string.Empty;
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Revoke the refresh token cookie and invalidate it in the database.</summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.RevokeTokenAsync(refreshToken);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string token) =>
        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        });

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}
