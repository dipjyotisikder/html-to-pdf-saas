using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTPDF.Infrastructure.Database.Entities;

public class OutboxMessage
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(100)]
    public required string MessageType { get; set; }

    [Required]
    public required string JobId { get; set; }

    [Required]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [EmailAddress]
    public required string EmailTo { get; set; }

    [Required]
    public required string EmailSubject { get; set; }

    [Required]
    public required string EmailBody { get; set; }

    public string? AttachmentPath { get; set; }
    public string? AttachmentFilename { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public int MaxRetryAttempts { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PermanentlyFailed = 4
}
