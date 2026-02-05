namespace HTPDF.Configuration;

/// <summary>
/// Configuration for JWT authentication.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Token issuer.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// Token audience.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Access token expiration in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
