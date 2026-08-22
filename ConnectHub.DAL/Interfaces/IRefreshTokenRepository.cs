using ConnectHub.Models.Entities;

namespace ConnectHub.DAL.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAllUserTokensAsync(Guid userId, string reason);
}
