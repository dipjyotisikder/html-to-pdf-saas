using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTPDF.Infrastructure.Database.Entities;

public class PdfJobEntity
{
    [Key]
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public required string HtmlContent { get; set; }

    public string Orientation { get; set; } = "Portrait";
    public string PaperSize { get; set; } = "A4";
    public string Filename { get; set; } = "Document.pdf";
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public bool EmailSent { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

public enum JobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
