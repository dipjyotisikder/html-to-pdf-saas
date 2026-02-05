namespace HTPDF.Configuration;

/// <summary>
/// Configuration for email service (SMTP).
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP server host.
    /// </summary>
    public required string SmtpHost { get; set; }

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// SMTP password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Sender email address.
    /// </summary>
    public required string FromEmail { get; set; }

    /// <summary>
    /// Sender display name.
    /// </summary>
    public string FromName { get; set; } = "HTML to PDF Service";
}
