namespace BFF.Configuration;

public sealed class IntegrationOptions
{
    public const string SectionName = "Integration";
    public string BaseUrl { get; init; } = string.Empty;
    public string ConnectHubPath { get; init; } = string.Empty;
    public string TokenPath { get; init; } = string.Empty;
    public string TokenRevokePath { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public int IntegrationTokenExpirationSafetyMarginSeconds { get; init; } = 30;
}

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    public string LoginPath { get; init; } = string.Empty;
    public string RegisterPath { get; init; } = string.Empty;
    public string ExternalLoginPath { get; init; } = string.Empty;
    public string RefreshPath { get; init; } = string.Empty;
    public string RevokePath { get; init; } = string.Empty;
    public string LogoutPath { get; init; } = string.Empty;
    public int AccessTokenExpirationSafetyMarginSeconds { get; init; } = 30;
}

public sealed class SessionOptions
{
    public const string SectionName = "Session";
    public string CookieName { get; init; } = "Yalla.Session";
    public int ExpirationMinutes { get; init; } = 60;
    public string SecurePolicy { get; init; } = "Always";
    public string SameSite { get; init; } = "None";
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; init; } = [];
}

public sealed class GoogleOptions
{
    public const string SectionName = "Google";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackPath { get; init; } = "/auth/google/callback";
    public string RedirectUri { get; init; } = string.Empty;
    public string FrontendRedirectUri { get; init; } = string.Empty;
}

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; init; } = string.Empty;
    public ModerationOptions Moderation { get; init; } = new();
}

public sealed class ModerationOptions
{
    public string Model { get; init; } = "omni-moderation-latest";
    public int TimeoutSeconds { get; init; } = 10;
}
