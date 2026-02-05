using HTPDF.Configuration;
using HTPDF.Data;
using HTPDF.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HTPDF.Services;

/// <summary>
/// Implementation of outbox service.
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly ApplicationDbContext _context;
    private readonly OutboxSettings _settings;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        ApplicationDbContext context,
        IOptions<OutboxSettings> settings,
        ILogger<OutboxService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task CreatePdfCompletedMessageAsync(string jobId, string userId, string email, string filename, string downloadUrl)
    {
        var message = new OutboxMessage
        {
            MessageType = "PdfCompleted",
            JobId = jobId,
            UserId = userId,
            EmailTo = email,
            EmailSubject = "Your PDF is Ready!",
            EmailBody = $"Your PDF '{filename}' has been generated successfully. Download it from: {downloadUrl}",
            AttachmentFilename = filename,
            MaxRetryAttempts = _settings.MaxRetryAttempts
        };

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Outbox message created for completed PDF job {JobId}", jobId);
    }

    /// <inheritdoc />
    public async Task CreatePdfFailedMessageAsync(string jobId, string userId, string email, string errorMessage)
    {
        var message = new OutboxMessage
        {
            MessageType = "PdfFailed",
            JobId = jobId,
            UserId = userId,
            EmailTo = email,
            EmailSubject = "PDF Generation Failed",
            EmailBody = $"Your PDF generation failed with error: {errorMessage}",
            MaxRetryAttempts = _settings.MaxRetryAttempts
        };

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Outbox message created for failed PDF job {JobId}", jobId);
    }

    /// <inheritdoc />
    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize)
    {
        var now = DateTime.UtcNow;

        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && 
                       (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task MarkAsCompletedAsync(string messageId)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Completed;
            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Outbox message {MessageId} marked as completed", messageId);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(string messageId, string errorMessage, int attemptNumber)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Pending; // Keep as pending for retry
            message.AttemptCount = attemptNumber;
            message.ErrorMessage = errorMessage;
            message.LastAttemptedAt = DateTime.UtcNow;

            // Calculate next retry time using exponential backoff
            var delayMinutes = _settings.BaseRetryDelayMinutes * Math.Pow(_settings.BackoffMultiplier, attemptNumber - 1);
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);

            await _context.SaveChangesAsync();

            _logger.LogWarning("Outbox message {MessageId} failed, attempt {Attempt}. Next retry at {NextRetry}",
                messageId, attemptNumber, message.NextRetryAt);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsPermanentlyFailedAsync(string messageId, string errorMessage)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.PermanentlyFailed;
            message.ErrorMessage = errorMessage;
            message.LastAttemptedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogError("Outbox message {MessageId} permanently failed: {Error}", messageId, errorMessage);
        }
    }
}
