using System.ComponentModel.DataAnnotations;

namespace HTPDF.Models.Auth;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address (used as username).
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string Password { get; set; }

    /// <summary>
    /// Password confirmation.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; set; }

    /// <summary>
    /// User's first name.
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }
}

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// User's password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; set; }
}

/// <summary>
/// Request model for refreshing JWT token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Expired or expiring access token.
    /// </summary>
    [Required]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Valid refresh token.
    /// </summary>
    [Required]
    public required string RefreshToken { get; set; }
}

/// <summary>
/// Response model for authentication operations.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (Bearer).
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// User's email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's roles.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Request model for external OAuth login.
/// </summary>
public class ExternalLoginRequest
{
    /// <summary>
    /// Provider name (e.g., "Google", "Microsoft").
    /// </summary>
    [Required]
    public required string Provider { get; set; }

    /// <summary>
    /// ID token from OAuth provider.
    /// </summary>
    [Required]
    public required string IdToken { get; set; }
}
