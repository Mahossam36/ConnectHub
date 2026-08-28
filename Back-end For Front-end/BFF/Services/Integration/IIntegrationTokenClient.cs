namespace BFF.Services.Integration;

public interface IIntegrationTokenClient
{
    Task<IntegrationTokenResult> AcquireAsync(CancellationToken cancellationToken = default);
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}

public sealed record IntegrationTokenResult(string Token, int? ExpiresInSeconds);
