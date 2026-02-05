using HTPDF.Configuration;
using HTPDF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HTPDF.Services;

/// <summary>
/// Implementation of file storage service.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _storageRootPath;

    public FileStorageService(
        IOptions<FileStorageSettings> settings,
        ApplicationDbContext context,
        ILogger<FileStorageService> logger,
        IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _context = context;
        _logger = logger;

        // Combine with web root or content root
        _storageRootPath = Path.Combine(environment.ContentRootPath, _settings.StoragePath);

        // Ensure storage directory exists
        if (!Directory.Exists(_storageRootPath))
        {
            Directory.CreateDirectory(_storageRootPath);
            _logger.LogInformation("Created storage directory: {Path}", _storageRootPath);
        }
    }

    /// <inheritdoc />
    public async Task<string> SavePdfAsync(byte[] pdfBytes, string filename)
    {
        try
        {
            if (pdfBytes.Length > _settings.MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes} bytes");
            }

            // Generate unique filename with timestamp
            var uniqueFilename = $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
            var filePath = Path.Combine(_storageRootPath, uniqueFilename);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            _logger.LogInformation("PDF saved successfully: {FilePath}, Size: {Size} bytes", filePath, pdfBytes.Length);

            // Return relative path
            return Path.Combine(_settings.StoragePath, uniqueFilename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF file: {Filename}", filename);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetPdfAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("PDF file not found: {FilePath}", fullPath);
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading PDF file: {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeletePdfAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("PDF file not found for deletion: {FilePath}", fullPath);
                return false;
            }

            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("PDF file deleted: {FilePath}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting PDF file: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredFilesAsync()
    {
        try
        {
            var expirationDate = DateTime.UtcNow;
            
            // Get expired jobs from database
            var expiredJobs = await _context.PdfJobs
                .Where(j => j.ExpiresAt != null && j.ExpiresAt < expirationDate && j.FilePath != null)
                .ToListAsync();

            var deletedCount = 0;

            foreach (var job in expiredJobs)
            {
                if (!string.IsNullOrEmpty(job.FilePath) && await DeletePdfAsync(job.FilePath))
                {
                    job.FilePath = null; // Clear file path in database
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} expired PDF files", deletedCount);
            }

            // Also clean up orphaned files (files not in database)
            var allFiles = Directory.GetFiles(_storageRootPath, "*.pdf");
            var filesInDb = await _context.PdfJobs
                .Where(j => j.FilePath != null)
                .Select(j => j.FilePath!)
                .ToListAsync();

            foreach (var file in allFiles)
            {
                var relativePath = Path.Combine(_settings.StoragePath, Path.GetFileName(file));
                if (!filesInDb.Contains(relativePath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc.AddDays(_settings.RetentionDays) < DateTime.UtcNow)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogInformation("Deleted orphaned file: {File}", file);
                    }
                }
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired files");
            return 0;
        }
    }

    /// <inheritdoc />
    public bool FileExists(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        return File.Exists(fullPath);
    }

    private string GetFullPath(string relativePath)
    {
        // If already absolute, return as-is
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        // Combine with content root
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }
}
