namespace HTPDF;

/// <summary>
/// Interface for creating PDF documents from HTML content.
/// </summary>
public interface IPdfMaker
{
    /// <summary>
    /// Method to create PDF from hardcoded data (for backward compatibility).
    /// </summary>
    /// <returns>PDF bytes.</returns>
    byte[] CreatePDF();

    /// <summary>
    /// Method to create PDF from custom HTML content.
    /// </summary>
    /// <param name="htmlContent">The HTML content to convert to PDF.</param>
    /// <param name="orientation">Paper orientation (Portrait or Landscape).</param>
    /// <param name="paperSize">Paper size (A4, A3, Letter, Legal).</param>
    /// <returns>PDF bytes.</returns>
    byte[] CreatePDF(string htmlContent, string orientation = "Portrait", string paperSize = "A4");

    /// <summary>
    /// Method to create chunked PDF from hardcoded data (for backward compatibility).
    /// </summary>
    /// <returns>PDF bytes.</returns>
    byte[] CreateChunkedPDF();
}
