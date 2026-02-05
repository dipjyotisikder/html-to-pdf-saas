namespace HTPDF.Configuration;

/// <summary>
/// Configuration for request size limits.
/// </summary>
public class RequestLimits
{
    /// <summary>
    /// Maximum HTML content size in bytes (default: 2MB).
    /// </summary>
    public int MaxHtmlSizeBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>
    /// Maximum PDF generation timeout in seconds (default: 30 seconds).
    /// </summary>
    public int MaxGenerationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum concurrent jobs per API key (default: 5).
    /// </summary>
    public int MaxConcurrentJobsPerKey { get; set; } = 5;
}
