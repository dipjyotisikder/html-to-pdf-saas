using MailKit.Net.Smtp;
using MimeKit;
using Polly;
using Polly.Retry;

namespace HTPDF.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _useSsl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _smtpHost = configuration["EmailSettings:SmtpHost"]!;
        _smtpPort = configuration.GetValue<int>("EmailSettings:SmtpPort");
        _useSsl = configuration.GetValue<bool>("EmailSettings:UseSsl");
        _username = configuration["EmailSettings:Username"]!;
        _password = configuration["EmailSettings:Password"]!;
        _fromEmail = configuration["EmailSettings:FromEmail"]!;
        _fromName = configuration.GetValue<string>("EmailSettings:FromName") ?? "HTML To PDF Service";
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Email Send Attempt {RetryCount} Failed. Waiting {TimeSpan} Before Next Retry", retryCount, timeSpan);
                });
    }

    public async Task<bool> SendAsync(string to, string subject, string body, string? attachmentPath = null, string? attachmentFilename = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    await builder.Attachments.AddAsync(attachmentPath, cancellationToken);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, _useSsl, cancellationToken);
                await client.AuthenticateAsync(_username, _password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email Sent Successfully To {ToEmail}", to);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed To Send Email To {ToEmail} After Retries", to);
            return false;
        }
    }
}
