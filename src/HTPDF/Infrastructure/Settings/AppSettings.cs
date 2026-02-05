namespace HTPDF.Infrastructure.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}

public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "HTML To PDF Service";
}

public class FileStorageSettings
{
    public const string SectionName = "FileStorageSettings";
    public string StoragePath { get; set; } = "Storage/PDFs";
    public int RetentionDays { get; set; } = 7;
    public long MaxFileSizeBytes { get; set; } = 52428800;
}

public class OutboxSettings
{
    public const string SectionName = "OutboxSettings";
    public int ProcessingIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 10;
    public int MaxRetryAttempts { get; set; } = 3;
    public int BaseRetryDelayMinutes { get; set; } = 1;
    public int BackoffMultiplier { get; set; } = 5;
}

public class ApiKeySettings
{
    public const string SectionName = "ApiKeySettings";
    public string HeaderName { get; set; } = "X-API-Key";
    public List<string> ValidApiKeys { get; set; } = new();
}

public class RequestLimits
{
    public const string SectionName = "RequestLimits";
    public long MaxHtmlSizeBytes { get; set; } = 2097152;
    public int MaxGenerationTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentJobsPerKey { get; set; } = 5;
}

public class ExternalAuthSettings
{
    public const string SectionName = "Authentication";
    public GoogleSettings Google { get; set; } = new();
    public MicrosoftSettings Microsoft { get; set; } = new();
}

public class GoogleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class MicrosoftSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
