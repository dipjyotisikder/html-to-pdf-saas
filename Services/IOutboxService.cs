using HTPDF.Data.Entities;

namespace HTPDF.Services;

/// <summary>
/// Service for managing outbox messages.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Creates an outbox message for PDF completion notification.
    /// </summary>
    Task CreatePdfCompletedMessageAsync(string jobId, string userId, string email, string filename, string downloadUrl);

    /// <summary>
    /// Creates an outbox message for PDF failure notification.
    /// </summary>
    Task CreatePdfFailedMessageAsync(string jobId, string userId, string email, string errorMessage);

    /// <summary>
    /// Retrieves pending outbox messages ready for processing.
    /// </summary>
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize);

    /// <summary>
    /// Marks a message as completed.
    /// </summary>
    Task MarkAsCompletedAsync(string messageId);

    /// <summary>
    /// Marks a message as failed and schedules retry.
    /// </summary>
    Task MarkAsFailedAsync(string messageId, string errorMessage, int attemptNumber);

    /// <summary>
    /// Marks a message as permanently failed.
    /// </summary>
    Task MarkAsPermanentlyFailedAsync(string messageId, string errorMessage);
}
