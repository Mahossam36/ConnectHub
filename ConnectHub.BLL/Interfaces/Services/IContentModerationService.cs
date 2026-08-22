using Ardalis.Result;

namespace ConnectHub.BLL.Interfaces.Services;

/// <summary>
/// Service abstraction for social/safety content moderation using OpenAI Moderation API.
/// </summary>
public interface IContentModerationService
{
    Task<Result<bool>> IsContentSafeAsync(string content, CancellationToken cancellationToken = default);
}
