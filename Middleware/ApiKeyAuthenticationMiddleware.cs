using HTPDF.Configuration;
using Microsoft.Extensions.Options;

namespace HTPDF.Middleware;

/// <summary>
/// Middleware for validating API key authentication.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeySettings _apiKeySettings;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next, 
        IOptions<ApiKeySettings> apiKeySettings,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _apiKeySettings = apiKeySettings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for Swagger and health check endpoints
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check if API key is provided
        if (!context.Request.Headers.TryGetValue(_apiKeySettings.HeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API key missing in request from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is missing" });
            return;
        }

        // Validate API key
        if (!_apiKeySettings.ValidApiKeys.Contains(extractedApiKey.ToString()))
        {
            _logger.LogWarning("Invalid API key attempt from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        // Store API key in HttpContext for later use (e.g., for logging or rate limiting per key)
        context.Items["ApiKey"] = extractedApiKey.ToString();

        await _next(context);
    }
}
