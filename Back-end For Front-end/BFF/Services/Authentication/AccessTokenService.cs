using System.Collections.Concurrent;
using System.Text.Json;
using BFF.Models.Sessions;
using BFF.Services.Sessions;
using BFF.Configuration;
using Microsoft.Extensions.Options;

namespace BFF.Services.Authentication;

public sealed class AccessTokenService(
    IApplicationTokenRefreshClient refreshClient,
    ISessionService sessionService,
    IOptions<AuthenticationOptions> options) : IAccessTokenService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks = new();
    private readonly AuthenticationOptions _options = options.Value;

    public async Task<UserSession?> GetSessionWithValidAccessTokenAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (HasUsableExpiration(session.AccessToken, DateTimeOffset.UtcNow.AddSeconds(_options.AccessTokenExpirationSafetyMarginSeconds)))
            return session;

        var refreshLock = RefreshLocks.GetOrAdd(session.SessionId, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            // A concurrent request may have already rotated and stored this session.
            var refreshed = await sessionService.GetAsync(session.SessionId, cancellationToken) ?? session;
            if (HasUsableExpiration(refreshed.AccessToken, DateTimeOffset.UtcNow.AddSeconds(_options.AccessTokenExpirationSafetyMarginSeconds)))
                return refreshed;

            var result = await refreshClient.RefreshAsync(refreshed.RefreshToken, cancellationToken);
            if (!result.Succeeded || result.Authentication is null)
                return null;

            var updated = refreshed with
            {
                AccessToken = result.Authentication.AccessToken,
                RefreshToken = result.Authentication.RefreshToken,
                ExpiresAt = result.Authentication.ExpiresAt,
                RefreshTokenExpiresAt = null
            };
            await sessionService.UpdateAsync(updated, cancellationToken);
            return updated;
        }
        finally { refreshLock.Release(); }
    }

    private static bool HasUsableExpiration(string token, DateTimeOffset threshold)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return document.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds) &&
                   DateTimeOffset.FromUnixTimeSeconds(seconds) > threshold;
        }
        catch (Exception) { return false; }
    }
}
