using BFF.Models.Sessions;

namespace BFF.Services.Sessions;

public interface ISessionStore
{
    Task StoreAsync(UserSession session, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<UserSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
