using Microsoft.AspNetCore.Identity;

namespace HTPDF.Data.Entities;

/// <summary>
/// Application user entity extending Identity user.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Date when user registered.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property for user's PDF jobs.
    /// </summary>
    public virtual ICollection<PdfJobEntity> PdfJobs { get; set; } = new List<PdfJobEntity>();

    /// <summary>
    /// Navigation property for user's refresh tokens.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
