namespace BFF.Services.Integration;

public interface IIntegrationTokenService
{
    Task<string> GetValidAsync(CancellationToken cancellationToken = default);
    Task RevokeAsync(CancellationToken cancellationToken = default);
}
