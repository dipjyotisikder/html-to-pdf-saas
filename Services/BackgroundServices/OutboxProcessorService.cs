using HTPDF.Configuration;
using HTPDF.Data.Entities;
using Microsoft.Extensions.Options;

namespace HTPDF.Services.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages with retry mechanism.
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSettings _settings;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxSettings> settings,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service started. Processing every {Interval} seconds",
            _settings.ProcessingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var messages = await outboxService.GetPendingMessagesAsync(_settings.BatchSize);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessMessageAsync(message, outboxService, emailService, fileStorage, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IOutboxService outboxService,
        IEmailService emailService,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing outbox message {MessageId}, Type: {Type}, Attempt: {Attempt}",
                message.Id, message.MessageType, message.AttemptCount + 1);

            bool emailSent = false;

            // Send email based on message type
            switch (message.MessageType)
            {
                case "PdfCompleted":
                    emailSent = await emailService.SendPdfCompletedEmailAsync(
                        message.EmailTo,
                        message.JobId,
                        message.AttachmentFilename ?? "document.pdf",
                        message.EmailBody.Contains("http") ? message.EmailBody.Split("from: ")[1] : $"/pdf/jobs/{message.JobId}/download");
                    break;

                case "PdfFailed":
                    var errorMsg = message.EmailBody.Contains("error:") ? message.EmailBody.Split("error: ")[1] : "Unknown error";
                    emailSent = await emailService.SendPdfFailedEmailAsync(
                        message.EmailTo,
                        message.JobId,
                        errorMsg);
                    break;

                default:
                    // Generic email send
                    emailSent = await emailService.SendEmailAsync(
                        message.EmailTo,
                        message.EmailSubject,
                        message.EmailBody,
                        message.AttachmentPath,
                        message.AttachmentFilename);
                    break;
            }

            if (emailSent)
            {
                await outboxService.MarkAsCompletedAsync(message.Id);
                _logger.LogInformation("Outbox message {MessageId} processed successfully", message.Id);
            }
            else
            {
                await HandleFailedMessageAsync(message, outboxService, "Email send failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
            await HandleFailedMessageAsync(message, outboxService, ex.Message);
        }
    }

    private async Task HandleFailedMessageAsync(OutboxMessage message, IOutboxService outboxService, string errorMessage)
    {
        var attemptNumber = message.AttemptCount + 1;

        if (attemptNumber >= message.MaxRetryAttempts)
        {
            await outboxService.MarkAsPermanentlyFailedAsync(message.Id, errorMessage);
            _logger.LogError("Outbox message {MessageId} permanently failed after {Attempts} attempts",
                message.Id, attemptNumber);
        }
        else
        {
            await outboxService.MarkAsFailedAsync(message.Id, errorMessage, attemptNumber);
            _logger.LogWarning("Outbox message {MessageId} failed, will retry (attempt {Attempt}/{Max})",
                message.Id, attemptNumber, message.MaxRetryAttempts);
        }
    }
}
