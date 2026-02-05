using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTPDF.Data.Entities;

/// <summary>
/// Represents an outbox message for reliable message delivery.
/// Implements the Outbox pattern for guaranteed message processing.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique message identifier.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Type of message (e.g., "PdfCompleted", "PdfFailed").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string MessageType { get; set; }

    /// <summary>
    /// Related job ID.
    /// </summary>
    [Required]
    public required string JobId { get; set; }

    /// <summary>
    /// User ID who owns this message.
    /// </summary>
    [Required]
    public required string UserId { get; set; }

    /// <summary>
    /// Navigation property to user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Email address to send to.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string EmailTo { get; set; }

    /// <summary>
    /// Email subject.
    /// </summary>
    [Required]
    public required string EmailSubject { get; set; }

    /// <summary>
    /// Email body content.
    /// </summary>
    [Required]
    public required string EmailBody { get; set; }

    /// <summary>
    /// File path of PDF attachment (if applicable).
    /// </summary>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Attachment filename.
    /// </summary>
    public string? AttachmentFilename { get; set; }

    /// <summary>
    /// Current processing status.
    /// </summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of last processing attempt.
    /// </summary>
    public DateTime? LastAttemptedAt { get; set; }

    /// <summary>
    /// Timestamp when next retry should be attempted.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Timestamp when message was successfully processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Status of an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PermanentlyFailed = 4
}
