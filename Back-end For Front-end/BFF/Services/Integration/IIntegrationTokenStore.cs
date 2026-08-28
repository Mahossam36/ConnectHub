namespace BFF.Services.Integration;

public interface IIntegrationTokenStore
{
    Task<string?> GetAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task RemoveAsync(CancellationToken cancellationToken = default);
}
