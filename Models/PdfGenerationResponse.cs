namespace HTPDF.Models;

/// <summary>
/// Response model for PDF generation requests.
/// </summary>
public class PdfGenerationResponse
{
    /// <summary>
    /// Unique job identifier.
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Current status of the PDF generation job.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Optional message providing additional information.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
