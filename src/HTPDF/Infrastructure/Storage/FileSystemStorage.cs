using HTPDF.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Infrastructure.Storage;

public class FileSystemStorage : IFileStorage
{
    private readonly string _storageRoot;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileSystemStorage> _logger;
    private readonly int _retentionDays;
    private readonly long _maxFileSize;

    public FileSystemStorage(
        IWebHostEnvironment environment,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<FileSystemStorage> logger)
    {
        var storagePath = configuration["FileStorageSettings:StoragePath"] ?? "Storage/PDFs";
        _storageRoot = Path.Combine(environment.ContentRootPath, storagePath);
        _context = context;
        _logger = logger;
        _retentionDays = configuration.GetValue<int>("FileStorageSettings:RetentionDays", 7);
        _maxFileSize = configuration.GetValue<long>("FileStorageSettings:MaxFileSizeBytes", 52428800);

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.LogInformation("Created Storage Directory: {Path}", _storageRoot);
        }
    }

    public async Task<string> SaveAsync(byte[] data, string filename, CancellationToken cancellationToken = default)
    {
        if (data.Length > _maxFileSize)
        {
            throw new InvalidOperationException($"File Size Exceeds Maximum Allowed Size Of {_maxFileSize} Bytes");
        }

        var uniqueFilename = $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
        var fullPath = Path.Combine(_storageRoot, uniqueFilename);

        await File.WriteAllBytesAsync(fullPath, data, cancellationToken);

        _logger.LogInformation("PDF Saved Successfully: {FilePath}, Size: {Size} Bytes", fullPath, data.Length);

        return Path.Combine("Storage/PDFs", uniqueFilename);
    }

    public async Task<byte[]?> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("PDF File Not Found: {FilePath}", fullPath);
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("PDF File Not Found For Deletion: {FilePath}", fullPath);
            return false;
        }

        await Task.Run(() => File.Delete(fullPath), cancellationToken);
        _logger.LogInformation("PDF File Deleted: {FilePath}", fullPath);
        return true;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expirationDate = DateTime.UtcNow;
        var expiredJobs = await _context.PdfJobs
            .Where(j => j.ExpiresAt != null && j.ExpiresAt < expirationDate && j.FilePath != null)
            .ToListAsync(cancellationToken);

        var deletedCount = 0;

        foreach (var job in expiredJobs)
        {
            if (!string.IsNullOrEmpty(job.FilePath) && await DeleteAsync(job.FilePath, cancellationToken))
            {
                job.FilePath = null;
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} Expired PDF Files", deletedCount);
        }

        return deletedCount;
    }

    public bool Exists(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        return File.Exists(fullPath);
    }

    private string GetFullPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }
}
