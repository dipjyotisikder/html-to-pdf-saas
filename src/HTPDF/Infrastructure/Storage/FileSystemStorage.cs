using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace HTPDF.Infrastructure.Storage;

public class FileSystemStorage : IFileStorage
{
    private readonly string _storageRoot;
    private readonly ApplicationDbContext _context;
    private readonly ILoggingService<FileSystemStorage> _logger;
    private readonly FileStorageSettings _settings;

    public FileSystemStorage(
        IWebHostEnvironment environment,
        ApplicationDbContext context,
        IOptions<FileStorageSettings> options,
        ILoggingService<FileSystemStorage> logger)
    {
        _settings = options.Value;
        _storageRoot = Path.Combine(environment.ContentRootPath, _settings.StoragePath);
        _context = context;
        _logger = logger;

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.LogInfo(LogMessages.Infrastructure.StorageDirectoryCreated, _storageRoot);
        }
    }


    public async Task<string> SaveAsync(byte[] data, string filename, CancellationToken cancellationToken = default)
    {
        if (data.Length > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File Size Exceeds Maximum Allowed Size Of {_settings.MaxFileSizeBytes} Bytes");
        }

        var uniqueFilename = $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
        var fullPath = Path.Combine(_storageRoot, uniqueFilename);

        await File.WriteAllBytesAsync(fullPath, data, cancellationToken);

        _logger.LogInfo(LogMessages.Infrastructure.PdfSaved, fullPath, data.Length);

        return Path.Combine(_settings.StoragePath, uniqueFilename);

    }


    public async Task<byte[]?> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning(LogMessages.Infrastructure.PdfNotFound, fullPath);
            return null;
        }


        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning(LogMessages.Infrastructure.PdfNotFoundForDeletion, fullPath);
            return false;
        }


        await Task.Run(() => File.Delete(fullPath), cancellationToken);
        _logger.LogInfo(LogMessages.Infrastructure.PdfDeleted, fullPath);
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
            _logger.LogInfo(LogMessages.Infrastructure.ExpiredPdfsDeleted, deletedCount);
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
