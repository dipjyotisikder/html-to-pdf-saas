using HTPDF.Models.Auth;
using HTPDF.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Controllers;

/// <summary>
/// Controller for authentication and authorization.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <returns>Authentication tokens if successful.</returns>
    /// <response code="200">Returns JWT tokens for the new user.</response>
    /// <response code="400">If registration fails or validation errors occur.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, response) = await _authService.RegisterAsync(request);

        if (!success)
        {
            return BadRequest(new { error = message });
        }

        _logger.LogInformation("User {Email} registered successfully", request.Email);

        return Ok(response);
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Authentication tokens if successful.</returns>
    /// <response code="200">Returns JWT tokens for the authenticated user.</response>
    /// <response code="400">If login fails.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, response) = await _authService.LoginAsync(request);

        if (!success)
        {
            return BadRequest(new { error = message });
        }

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return Ok(response);
    }

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New authentication tokens.</returns>
    /// <response code="200">Returns new JWT tokens.</response>
    /// <response code="400">If refresh fails.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, response) = await _authService.RefreshTokenAsync(request);

        if (!success)
        {
            return BadRequest(new { error = message });
        }

        _logger.LogInformation("Token refreshed successfully");

        return Ok(response);
    }

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="request">Token to revoke.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">If token was revoked successfully.</response>
    /// <response code="400">If revocation fails.</response>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        var revoked = await _authService.RevokeTokenAsync(request.RefreshToken);

        if (!revoked)
        {
            return BadRequest(new { error = "Failed to revoke token" });
        }

        _logger.LogInformation("Token revoked");

        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Authenticates user via external OAuth provider (Google, Microsoft).
    /// </summary>
    /// <param name="request">External login request.</param>
    /// <returns>Authentication tokens.</returns>
    /// <response code="200">Returns JWT tokens.</response>
    /// <response code="400">If external login fails.</response>
    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, response) = await _authService.ExternalLoginAsync(request);

        if (!success)
        {
            return BadRequest(new { error = message });
        }

        _logger.LogInformation("External login successful via {Provider}", request.Provider);

        return Ok(response);
    }
}
