namespace HTPDF.Services;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with optional attachment.
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null, string? attachmentFilename = null);

    /// <summary>
    /// Sends an email notifying that PDF generation is complete.
    /// </summary>
    Task<bool> SendPdfCompletedEmailAsync(string toEmail, string jobId, string filename, string downloadUrl);

    /// <summary>
    /// Sends an email notifying that PDF generation failed.
    /// </summary>
    Task<bool> SendPdfFailedEmailAsync(string toEmail, string jobId, string errorMessage);
}
