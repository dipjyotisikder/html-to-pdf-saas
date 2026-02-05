namespace HTPDF.Configuration;

/// <summary>
/// Configuration for API key authentication.
/// </summary>
public class ApiKeySettings
{
    /// <summary>
    /// List of valid API keys.
    /// </summary>
    public List<string> ValidApiKeys { get; set; } = new();

    /// <summary>
    /// Name of the header containing the API key.
    /// </summary>
    public string HeaderName { get; set; } = "X-API-Key";
}
