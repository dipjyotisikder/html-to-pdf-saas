using Ganss.Xss;

namespace HTPDF.Services;

/// <summary>
/// Service for sanitizing HTML content to prevent XSS attacks.
/// </summary>
public interface IHtmlSanitizerService
{
    /// <summary>
    /// Sanitizes HTML content to remove potentially malicious scripts and elements.
    /// </summary>
    /// <param name="htmlContent">The HTML content to sanitize.</param>
    /// <returns>Sanitized HTML content.</returns>
    string Sanitize(string htmlContent);
}

/// <summary>
/// Implementation of HTML sanitization service.
/// </summary>
public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        
        // Configure allowed tags and attributes
        _sanitizer.AllowedTags.Add("table");
        _sanitizer.AllowedTags.Add("thead");
        _sanitizer.AllowedTags.Add("tbody");
        _sanitizer.AllowedTags.Add("tr");
        _sanitizer.AllowedTags.Add("th");
        _sanitizer.AllowedTags.Add("td");
        _sanitizer.AllowedTags.Add("div");
        _sanitizer.AllowedTags.Add("span");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("img");
        
        // Allow style attributes for formatting
        _sanitizer.AllowedAttributes.Add("style");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("border");
        _sanitizer.AllowedAttributes.Add("cellspacing");
        _sanitizer.AllowedAttributes.Add("cellpadding");
        _sanitizer.AllowedAttributes.Add("colspan");
        _sanitizer.AllowedAttributes.Add("rowspan");
        _sanitizer.AllowedAttributes.Add("width");
        _sanitizer.AllowedAttributes.Add("height");
        _sanitizer.AllowedAttributes.Add("align");
        
        // For images
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        
        // Allow data URI scheme for embedded images
        _sanitizer.AllowedSchemes.Add("data");
    }

    /// <inheritdoc />
    public string Sanitize(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML content cannot be null or empty.", nameof(htmlContent));
        }

        return _sanitizer.Sanitize(htmlContent);
    }
}
