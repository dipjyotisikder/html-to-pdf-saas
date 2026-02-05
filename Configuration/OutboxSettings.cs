namespace HTPDF.Configuration;

/// <summary>
/// Configuration for outbox pattern and retry mechanism.
/// </summary>
public class OutboxSettings
{
    /// <summary>
    /// Interval in seconds for processing outbox messages.
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of messages to process in one batch.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Maximum retry attempts for failed messages.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in minutes for exponential backoff (first retry).
    /// </summary>
    public int BaseRetryDelayMinutes { get; set; } = 1;

    /// <summary>
    /// Multiplier for exponential backoff.
    /// </summary>
    public int BackoffMultiplier { get; set; } = 5;
}
