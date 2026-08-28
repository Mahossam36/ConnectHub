using BFF.Models.Auth;
using BFF.Models.Sessions;

namespace BFF.Services.Sessions;

public interface ISessionService
{
    Task<UserSession> CreateAsync(UpstreamAuthResponse authentication, CancellationToken cancellationToken = default);
    Task<UserSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetCurrentAsync(HttpContext context, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task RemoveCurrentAsync(HttpContext context, CancellationToken cancellationToken = default);
    void SetCookie(HttpContext context, string sessionId);
    void ClearCookie(HttpContext context);
}
