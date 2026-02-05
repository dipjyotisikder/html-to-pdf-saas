using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Email;
using HTPDF.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;


namespace HTPDF.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggingService<OutboxProcessor> _logger;
    private readonly int _processingIntervalSeconds;
    private readonly int _batchSize;
    private readonly int _maxRetryAttempts;
    private readonly int _baseRetryDelayMinutes;
    private readonly int _backoffMultiplier;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILoggingService<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _processingIntervalSeconds = configuration.GetValue<int>("OutboxSettings:ProcessingIntervalSeconds", 30);
        _batchSize = configuration.GetValue<int>("OutboxSettings:BatchSize", 10);
        _maxRetryAttempts = configuration.GetValue<int>("OutboxSettings:MaxRetryAttempts", 3);
        _baseRetryDelayMinutes = configuration.GetValue<int>("OutboxSettings:BaseRetryDelayMinutes", 1);
        _backoffMultiplier = configuration.GetValue<int>("OutboxSettings:BackoffMultiplier", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo(LogMessages.Infrastructure.OutboxProcessorStarted, _processingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.Infrastructure.OutboxProcessingError);
            }

            await Task.Delay(TimeSpan.FromSeconds(_processingIntervalSeconds), stoppingToken);
        }
    }


    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = DateTime.UtcNow;
        var messages = await context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInfo(LogMessages.Infrastructure.ProcessingOutboxCount, messages.Count);

        foreach (var message in messages)

        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessMessageAsync(message, context, emailSender, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, ApplicationDbContext context, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo(LogMessages.Infrastructure.ProcessingOutboxMessage, message.Id, message.MessageType, message.AttemptCount + 1);

            var emailSent = await emailSender.SendAsync(
                message.EmailTo,
                message.EmailSubject,
                BuildEmailBody(message),
                message.AttachmentPath,
                message.AttachmentFilename,
                cancellationToken
            );

            if (emailSent)
            {
                message.Status = OutboxMessageStatus.Completed;
                message.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInfo(LogMessages.Infrastructure.OutboxMessageProcessed, message.Id);
            }
            else
            {
                await HandleFailureAsync(message, context, "Email Send Failed", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.Infrastructure.OutboxMessageProcessingError, message.Id);
            await HandleFailureAsync(message, context, ex.Message, cancellationToken);
        }
    }

    private async Task HandleFailureAsync(OutboxMessage message, ApplicationDbContext context, string errorMessage, CancellationToken cancellationToken)
    {
        message.AttemptCount++;
        message.ErrorMessage = errorMessage;
        message.LastAttemptedAt = DateTime.UtcNow;

        if (message.AttemptCount >= _maxRetryAttempts)
        {
            message.Status = OutboxMessageStatus.PermanentlyFailed;
            _logger.LogError(LogMessages.Infrastructure.OutboxMessagePermanentlyFailed, message.Id, message.AttemptCount);
        }
        else
        {
            var delayMinutes = _baseRetryDelayMinutes * Math.Pow(_backoffMultiplier, message.AttemptCount - 1);
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            _logger.LogWarning(LogMessages.Infrastructure.OutboxMessageRetry,
                message.Id, message.AttemptCount, _maxRetryAttempts, message.NextRetryAt);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEmailBody(OutboxMessage message)
    {
        return message.MessageType switch
        {
            "PdfCompleted" => $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #4CAF50;'>? PDF Generation Complete!</h2>
                    <p>{message.EmailBody}</p>
                    <p><strong>Job ID:</strong> {message.JobId}</p>
                    <p>Your PDF Will Be Available For Download For The Next 7 Days.</p>
                </body>
                </html>",

            "PdfFailed" => $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #f44336;'>? PDF Generation Failed</h2>
                    <p>{message.EmailBody}</p>
                    <p><strong>Job ID:</strong> {message.JobId}</p>
                    <p>Please Try Again Or Contact Support If The Problem Persists.</p>
                </body>
                </html>",

            _ => message.EmailBody
        };
    }
}
