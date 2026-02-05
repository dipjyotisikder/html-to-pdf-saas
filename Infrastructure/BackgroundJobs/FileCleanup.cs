using HTPDF.Infrastructure.Storage;

namespace HTPDF.Infrastructure.BackgroundJobs;

public class FileCleanup : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileCleanup> _logger;
    private const int CleanupIntervalHours = 6;

    public FileCleanup(IServiceScopeFactory scopeFactory, ILogger<FileCleanup> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File Cleanup Service Started. Running Every {Interval} Hours", CleanupIntervalHours);

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
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error During File Cleanup");
            }
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        _logger.LogInformation("Starting File Cleanup");

        var deletedCount = await fileStorage.DeleteExpiredAsync(cancellationToken);

        _logger.LogInformation("File Cleanup Completed. Deleted {Count} Files", deletedCount);
    }
}
