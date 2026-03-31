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
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
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
        Response.Cookies.Append(RefreshTokenCookie, token, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, BuildCookieOptions(DateTimeOffset.UnixEpoch));

    /// <summary>
    /// Development : SameSite=Strict, Secure=false  (HTTP localhost)
    /// Production  : SameSite=None,   Secure=true   (HTTPS, works cross-origin through SWA proxy)
    /// </summary>
    private CookieOptions BuildCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        SameSite = _env.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None,
        Secure   = !_env.IsDevelopment(),
        Expires  = expires,
        Path     = "/"
    };
}
