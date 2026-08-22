using System.Net;
using System.Text.RegularExpressions;
using ConnectHub.BLL.Interfaces.Services;

namespace ConnectHub.BLL.Services;

/// <summary>
/// Provides XSS sanitization for plain-text and user-submitted inputs.
/// </summary>
public class XssSanitizerService : IXssSanitizerService
{
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip HTML tags for plain-text content
        var stripped = HtmlTagRegex.Replace(input, string.Empty);

        // Normalize and decode HTML entities to avoid double-encoding issues while keeping it pure plain text
        var normalized = WebUtility.HtmlDecode(stripped).Trim();

        return normalized;
    }
}
