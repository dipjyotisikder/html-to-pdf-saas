using HTPDF.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;

namespace HTPDF.Services;

/// <summary>
/// Implementation of email service using MailKit with Polly retry.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;

        // Configure retry policy: 3 retries with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Email send attempt {RetryCount} failed. Waiting {TimeSpan} before next retry",
                        retryCount, timeSpan);
                });
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null, string? attachmentFilename = null)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };

                // Add attachment if provided
                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var filename = attachmentFilename ?? Path.GetFileName(attachmentPath);
                    await builder.Attachments.AddAsync(attachmentPath);
                    _logger.LogInformation("Email attachment added: {Filename}", filename);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.UseSsl);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} after retries", toEmail);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendPdfCompletedEmailAsync(string toEmail, string jobId, string filename, string downloadUrl)
    {
        var subject = "Your PDF is Ready!";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; 
                              color: white; text-decoration: none; border-radius: 4px; margin-top: 20px; }}
                    .footer {{ margin-top: 20px; padding: 20px; background-color: #f1f1f1; text-align: center; 
                              font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>? PDF Generation Complete!</h1>
                    </div>
                    <div class='content'>
                        <p>Hello,</p>
                        <p>Your PDF document has been successfully generated and is ready for download.</p>
                        <p><strong>Job ID:</strong> {jobId}</p>
                        <p><strong>Filename:</strong> {filename}</p>
                        <p>Your PDF will be available for download for the next 7 days.</p>
                        <a href='{downloadUrl}' class='button'>Download PDF</a>
                    </div>
                    <div class='footer'>
                        <p>This is an automated message from HTML to PDF Service.</p>
                        <p>If you did not request this PDF, please ignore this email.</p>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task<bool> SendPdfFailedEmailAsync(string toEmail, string jobId, string errorMessage)
    {
        var subject = "PDF Generation Failed";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .error-box {{ background-color: #ffebee; border-left: 4px solid #f44336; 
                                  padding: 15px; margin: 20px 0; }}
                    .footer {{ margin-top: 20px; padding: 20px; background-color: #f1f1f1; text-align: center; 
                              font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>? PDF Generation Failed</h1>
                    </div>
                    <div class='content'>
                        <p>Hello,</p>
                        <p>Unfortunately, we were unable to generate your PDF document.</p>
                        <p><strong>Job ID:</strong> {jobId}</p>
                        <div class='error-box'>
                            <strong>Error:</strong> {errorMessage}
                        </div>
                        <p>Please try again or contact support if the problem persists.</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated message from HTML to PDF Service.</p>
                        <p>Need help? Contact our support team.</p>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }
}
