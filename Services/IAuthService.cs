using HTPDF.Models.Auth;

namespace HTPDF.Services;

/// <summary>
/// Service for authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<(bool Success, string Message, AuthResponse? Response)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    Task<(bool Success, string Message, AuthResponse? Response)> LoginAsync(LoginRequest request);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    Task<(bool Success, string Message, AuthResponse? Response)> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Authenticates a user via external OAuth provider.
    /// </summary>
    Task<(bool Success, string Message, AuthResponse? Response)> ExternalLoginAsync(ExternalLoginRequest request);
}
