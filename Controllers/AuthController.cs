using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApi.DTOs.Auth;
using TaskApi.Services;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName =
        "__Secure-TaskApi.RefreshToken";

    private readonly IAuthService _authService;
    private readonly IAntiforgery _antiforgery;

    public AuthController(
        IAuthService authService,
        IAntiforgery antiforgery)
    {
        _authService = authService;
        _antiforgery = antiforgery;
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new CsrfTokenResponseDto
        {
            Token = tokens.RequestToken ?? string.Empty
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            dto,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => new
                {
                    e.Code,
                    e.Description
                })
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new { message = "Registration successful." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!await ValidateCsrfAsync())
        {
            return BadRequest(new
            {
                message = "Invalid anti-forgery token."
            });
        }

        var session = await _authService.LoginAsync(
            dto,
            cancellationToken);

        if (session is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        SetRefreshCookie(
            session.RefreshToken,
            session.RefreshTokenExpiresAtUtc);

        return Ok(new AuthResponseDto
        {
            AccessToken = session.AccessToken,
            ExpiresAtUtc = session.AccessTokenExpiresAtUtc
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        CancellationToken cancellationToken)
    {
        if (!await ValidateCsrfAsync())
        {
            return BadRequest(new
            {
                message = "Invalid anti-forgery token."
            });
        }

        if (!Request.Cookies.TryGetValue(
            RefreshCookieName,
            out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            DeleteRefreshCookie();

            return Unauthorized();
        }

        var session = await _authService.RefreshAsync(
            refreshToken,
            cancellationToken);

        if (session is null)
        {
            DeleteRefreshCookie();

            return Unauthorized(new
            {
                message = "Refresh token is invalid or expired."
            });
        }

        SetRefreshCookie(
            session.RefreshToken,
            session.RefreshTokenExpiresAtUtc);

        return Ok(new AuthResponseDto
        {
            AccessToken = session.AccessToken,
            ExpiresAtUtc = session.AccessTokenExpiresAtUtc
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        if (!await ValidateCsrfAsync())
        {
            return BadRequest(new
            {
                message = "Invalid anti-forgery token."
            });
        }

        if (Request.Cookies.TryGetValue(
            RefreshCookieName,
            out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.RevokeAsync(
                refreshToken,
                cancellationToken);
        }

        DeleteRefreshCookie();

        return NoContent();
    }

    private async Task<bool> ValidateCsrfAsync()
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private void SetRefreshCookie(
        string refreshToken,
        DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Path = "/api/auth",
                Expires = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        expiresAtUtc,
                        DateTimeKind.Utc))
            });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(
            RefreshCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/api/auth"
            });
    }
}