using System.Text.Json;
using BFF.Configuration;
using Microsoft.Extensions.Options;

namespace BFF.Services.Integration;

public sealed class IntegrationTokenService(
    IIntegrationTokenClient tokenClient,
    IIntegrationTokenStore tokenStore,
    IOptions<IntegrationOptions> options) : IIntegrationTokenService
{
    private static readonly SemaphoreSlim AcquisitionLock = new(1, 1);
    private readonly IntegrationOptions _options = options.Value;

    public async Task<string> GetValidAsync(CancellationToken cancellationToken = default)
    {
        var cachedToken = await tokenStore.GetAsync(cancellationToken);
        if (IsUsable(cachedToken, out _)) return cachedToken!;

        await AcquisitionLock.WaitAsync(cancellationToken);
        try
        {
            cachedToken = await tokenStore.GetAsync(cancellationToken);
            if (IsUsable(cachedToken, out _)) return cachedToken!;

            var result = await tokenClient.AcquireAsync(cancellationToken);
            var validJwt = IsUsable(result.Token, out var expiration);
            if (!validJwt && (result.ExpiresInSeconds is null || result.ExpiresInSeconds <= _options.IntegrationTokenExpirationSafetyMarginSeconds))
                throw new InvalidOperationException("WSO2 returned an integration token without a usable expiration.");

            var lifetime = validJwt
                ? expiration - DateTimeOffset.UtcNow
                : TimeSpan.FromSeconds(result.ExpiresInSeconds!.Value - _options.IntegrationTokenExpirationSafetyMarginSeconds);
            await tokenStore.SetAsync(result.Token, lifetime, cancellationToken);
            return result.Token;
        }
        finally { AcquisitionLock.Release(); }
    }

    public async Task RevokeAsync(CancellationToken cancellationToken = default)
    {
        await AcquisitionLock.WaitAsync(cancellationToken);
        try
        {
            var token = await tokenStore.GetAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token)) return;

            await tokenClient.RevokeAsync(token, cancellationToken);
            await tokenStore.RemoveAsync(cancellationToken);
        }
        finally { AcquisitionLock.Release(); }
    }

    private bool IsUsable(string? token, out DateTimeOffset expiration)
    {
        expiration = default;
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!document.RootElement.TryGetProperty("exp", out var exp) || !exp.TryGetInt64(out var seconds)) return false;
            expiration = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return expiration > DateTimeOffset.UtcNow.AddSeconds(_options.IntegrationTokenExpirationSafetyMarginSeconds);
        }
        catch (Exception) { return false; }
    }
}
