using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Settings;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;


namespace HTPDF.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILoggingService<SmtpEmailSender> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILoggingService<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, LogMessages.Infrastructure.EmailSendRetry, retryCount, timeSpan);
                });
    }


    public async Task<bool> SendAsync(string to, string subject, string body, string? attachmentPath = null, string? attachmentFilename = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    await builder.Attachments.AddAsync(attachmentPath, cancellationToken);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl, cancellationToken);
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInfo(LogMessages.Infrastructure.EmailSentSuccess, to);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.Infrastructure.EmailSendFailed, to);
            return false;
        }
    }
}

