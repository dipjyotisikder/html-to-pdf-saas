using HTPDF.Data.Entities;

namespace HTPDF.Models;

/// <summary>
/// DTO for PDF generation job (for backward compatibility with in-memory operations).
/// </summary>
public class PdfJob
{
    /// <summary>
    /// Unique job identifier.
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// The generated PDF bytes (available when status is Completed).
    /// </summary>
    public byte[]? PdfBytes { get; set; }

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the job was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Filename for the PDF.
    /// </summary>
    public string? Filename { get; set; }
}
