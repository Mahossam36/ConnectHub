using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.DAL.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                .SetProperty(rt => rt.RevokedReason, reason));
    }
}
