namespace ConnectHub.BLL.Interfaces.Services;

/// <summary>
/// Service abstraction for input sanitization and XSS prevention.
/// </summary>
public interface IXssSanitizerService
{
    string Sanitize(string? input);
}
