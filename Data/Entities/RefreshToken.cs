using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTPDF.Data.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID who owns this token.
    /// </summary>
    [Required]
    public required string UserId { get; set; }

    /// <summary>
    /// Navigation property to user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// The refresh token value.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string Token { get; set; }

    /// <summary>
    /// JWT ID that this refresh token is associated with.
    /// </summary>
    [Required]
    public required string JwtId { get; set; }

    /// <summary>
    /// Whether the token has been used.
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Timestamp when token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
