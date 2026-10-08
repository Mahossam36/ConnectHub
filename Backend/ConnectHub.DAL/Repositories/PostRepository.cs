using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.DAL.Repositories;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Post?> GetWithDetailsAsync(Guid postId)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == postId);
    }

    public async Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId)
    {
        return await _context.PostLikes
            .AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);
    }

    public async Task AddLikeAsync(PostLike like)
    {
        await _context.PostLikes.AddAsync(like);
    }

    public async Task RemoveLikeAsync(Guid postId, Guid userId)
    {
        await _context.PostLikes
            .Where(pl => pl.PostId == postId && pl.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
