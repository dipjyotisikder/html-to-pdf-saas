namespace HTPDF.Services;

/// <summary>
/// Service for managing PDF file storage.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves PDF bytes to file system.
    /// </summary>
    Task<string> SavePdfAsync(byte[] pdfBytes, string filename);

    /// <summary>
    /// Retrieves PDF bytes from file system.
    /// </summary>
    Task<byte[]?> GetPdfAsync(string filePath);

    /// <summary>
    /// Deletes a PDF file.
    /// </summary>
    Task<bool> DeletePdfAsync(string filePath);

    /// <summary>
    /// Deletes expired PDF files based on retention policy.
    /// </summary>
    Task<int> DeleteExpiredFilesAsync();

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    bool FileExists(string filePath);
}
