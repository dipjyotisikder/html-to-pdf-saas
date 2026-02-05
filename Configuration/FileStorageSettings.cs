namespace HTPDF.Configuration;

/// <summary>
/// Configuration for file storage.
/// </summary>
public class FileStorageSettings
{
    /// <summary>
    /// Root directory for storing PDF files.
    /// </summary>
    public string StoragePath { get; set; } = "Storage/PDFs";

    /// <summary>
    /// Number of days to retain PDF files before deletion.
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Maximum file size in bytes (default: 50MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
}
