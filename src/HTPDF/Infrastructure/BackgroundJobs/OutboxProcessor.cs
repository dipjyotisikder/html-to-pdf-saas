using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Email;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace HTPDF.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggingService<OutboxProcessor> _logger;
    private readonly OutboxSettings _settings;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxSettings> options,
        ILoggingService<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo(LogMessages.Infrastructure.OutboxProcessorStarted, _settings.ProcessingIntervalSeconds);

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

            await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingIntervalSeconds), stoppingToken);
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
            .Take(_settings.BatchSize)
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

        if (message.AttemptCount >= _settings.MaxRetryAttempts)
        {
            message.Status = OutboxMessageStatus.PermanentlyFailed;
            _logger.LogError(LogMessages.Infrastructure.OutboxMessagePermanentlyFailed, message.Id, message.AttemptCount);
        }
        else
        {
            var delayMinutes = _settings.BaseRetryDelayMinutes * Math.Pow(_settings.BackoffMultiplier, message.AttemptCount - 1);
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            _logger.LogWarning(LogMessages.Infrastructure.OutboxMessageRetry,
                message.Id, message.AttemptCount, _settings.MaxRetryAttempts, message.NextRetryAt);
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
