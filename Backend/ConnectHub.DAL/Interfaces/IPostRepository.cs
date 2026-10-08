using ConnectHub.Models.Entities;

namespace ConnectHub.DAL.Interfaces;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetWithDetailsAsync(Guid postId);
    Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId);
    Task AddLikeAsync(PostLike like);
    Task RemoveLikeAsync(Guid postId, Guid userId);
}
