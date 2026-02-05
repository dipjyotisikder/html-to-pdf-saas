using System.ComponentModel.DataAnnotations;

namespace HTPDF.Models;

/// <summary>
/// Request model for PDF generation from custom HTML.
/// </summary>
public class PdfGenerationRequest
{
    /// <summary>
    /// The HTML content to convert to PDF.
    /// </summary>
    [Required(ErrorMessage = "HTML content is required")]
    [StringLength(2097152, ErrorMessage = "HTML content cannot exceed 2MB")]
    public required string HtmlContent { get; set; }

    /// <summary>
    /// Optional filename for the generated PDF.
    /// </summary>
    [StringLength(255, ErrorMessage = "Filename cannot exceed 255 characters")]
    public string? Filename { get; set; }

    /// <summary>
    /// Paper orientation (Portrait or Landscape).
    /// </summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>
    /// Paper size (A4, A3, Letter, Legal).
    /// </summary>
    public string PaperSize { get; set; } = "A4";
}
