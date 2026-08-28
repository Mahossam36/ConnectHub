namespace BFF.Services.Moderation;

public interface IContentModerationService
{
    Task<ContentModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default);
}

public sealed record ContentModerationResult(bool IsAllowed, bool IsUnavailable)
{
    public static ContentModerationResult Allowed { get; } = new(true, false);
    public static ContentModerationResult Flagged { get; } = new(false, false);
    public static ContentModerationResult Unavailable { get; } = new(false, true);
}
