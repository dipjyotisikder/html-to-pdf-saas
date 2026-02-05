using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTPDF.Data.Entities;

/// <summary>
/// Represents a PDF generation job stored in the database.
/// </summary>
public class PdfJobEntity
{
    /// <summary>
    /// Unique job identifier.
    /// </summary>
    [Key]
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// User ID who created this job.
    /// </summary>
    [Required]
    public required string UserId { get; set; }

    /// <summary>
    /// Navigation property to user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// HTML content to convert (sanitized).
    /// </summary>
    [Required]
    public required string HtmlContent { get; set; }

    /// <summary>
    /// Paper orientation.
    /// </summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>
    /// Paper size.
    /// </summary>
    public string PaperSize { get; set; } = "A4";

    /// <summary>
    /// Desired filename for the PDF.
    /// </summary>
    public string Filename { get; set; } = "document.pdf";

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// File path where PDF is stored (if completed).
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when job was completed or failed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Whether email has been sent.
    /// </summary>
    public bool EmailSent { get; set; } = false;

    /// <summary>
    /// Timestamp when PDF file will expire and be deleted.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Status of a PDF generation job.
/// </summary>
public enum JobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
