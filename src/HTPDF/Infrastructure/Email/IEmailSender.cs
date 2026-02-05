namespace HTPDF.Infrastructure.Email;

public interface IEmailSender
{
    Task<bool> SendAsync(string to, string subject, string body, string? attachmentPath = null, string? attachmentFilename = null, CancellationToken cancellationToken = default);
}
