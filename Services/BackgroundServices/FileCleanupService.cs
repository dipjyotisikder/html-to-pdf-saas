using HTPDF.Configuration;
using Microsoft.Extensions.Options;

namespace HTPDF.Services.BackgroundServices;

/// <summary>
/// Background service that periodically deletes expired PDF files.
/// </summary>
public class FileCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FileStorageSettings _settings;
    private readonly ILogger<FileCleanupService> _logger;
    private const int CleanupIntervalHours = 6; // Run cleanup every 6 hours

    public FileCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<FileStorageSettings> settings,
        ILogger<FileCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File Cleanup Service started. Running every {Interval} hours", CleanupIntervalHours);

        // Run immediately on startup
        await PerformCleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(CleanupIntervalHours), stoppingToken);
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup");
            }
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        _logger.LogInformation("Starting file cleanup. Retention: {Days} days", _settings.RetentionDays);

        var deletedCount = await fileStorage.DeleteExpiredFilesAsync();

        _logger.LogInformation("File cleanup completed. Deleted {Count} files", deletedCount);
    }
}
