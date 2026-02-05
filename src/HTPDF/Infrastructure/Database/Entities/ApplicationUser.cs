using Microsoft.AspNetCore.Identity;

namespace HTPDF.Infrastructure.Database.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PdfJobEntity> PdfJobs { get; set; } = new List<PdfJobEntity>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
