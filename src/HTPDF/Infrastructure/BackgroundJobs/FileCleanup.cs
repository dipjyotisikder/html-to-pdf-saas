using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Storage;

namespace HTPDF.Infrastructure.BackgroundJobs;

public class FileCleanup : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggingService<FileCleanup> _logger;
    private const int CleanupIntervalHours = 6;

    public FileCleanup(IServiceScopeFactory scopeFactory, ILoggingService<FileCleanup> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo(LogMessages.Infrastructure.FileCleanupServiceStarted, CleanupIntervalHours);

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
                _logger.LogError(ex, LogMessages.Infrastructure.FileCleanupError);
            }
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        _logger.LogInfo(LogMessages.Infrastructure.FileCleanupStarted);

        var deletedCount = await fileStorage.DeleteExpiredAsync(cancellationToken);

        _logger.LogInfo(LogMessages.Infrastructure.FileCleanupCompleted, deletedCount);
    }
}

