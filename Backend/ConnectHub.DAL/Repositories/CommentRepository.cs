using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace ConnectHub.DAL.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context)
        {

        }

        public IQueryable<Comment> GetPostComments(Guid postId)
        {
            return _context.Comments
                .AsNoTracking()
                .Where(c =>
                    c.PostId == postId &&
                    c.ParentCommentId == null);
              
        }

        public async Task<bool> HasUserLikedCommentAsync(Guid commentId, Guid userId)
        {
            return await _context.CommentLikes
                .AnyAsync(cl =>
                    cl.CommentId == commentId &&
                    cl.UserId == userId);
        }

        public async Task AddLikeAsync(CommentLike like)
        {
            await _context.CommentLikes.AddAsync(like);
        }

        public async Task RemoveLikeAsync(Guid commentId, Guid userId)
        {
            await _context.CommentLikes
                .Where(cl =>
                    cl.CommentId == commentId &&
                    cl.UserId == userId)
                .ExecuteDeleteAsync();
        }


    }


}
