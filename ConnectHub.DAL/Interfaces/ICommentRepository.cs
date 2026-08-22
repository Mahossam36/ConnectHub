using ConnectHub.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Interfaces
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        IQueryable<Comment> GetPostComments(Guid postId);
        Task<bool> HasUserLikedCommentAsync(Guid commentId, Guid userId);
        Task AddLikeAsync(CommentLike like);
        Task RemoveLikeAsync(Guid commentId, Guid userId);
    }
}
