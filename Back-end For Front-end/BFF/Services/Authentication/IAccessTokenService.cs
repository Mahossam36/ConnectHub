using BFF.Models.Sessions;

namespace BFF.Services.Authentication;

public interface IAccessTokenService
{
    Task<UserSession?> GetSessionWithValidAccessTokenAsync(UserSession session, CancellationToken cancellationToken = default);
}
