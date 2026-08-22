using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Comments;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGenericRepository<CommentLike> _commentLikeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly IContentModerationService _contentModeration;
    private readonly IRealTimeNotificationService _realTimeNotification;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        IGenericRepository<CommentLike> commentLikeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        IXssSanitizerService xssSanitizer,
        IContentModerationService contentModeration,
        IRealTimeNotificationService realTimeNotification,
        INotificationService notificationService,
        ILogger<CommentService> logger)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _commentLikeRepository = commentLikeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _xssSanitizer = xssSanitizer;
        _contentModeration = contentModeration;
        _realTimeNotification = realTimeNotification;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<CommentResponseDto>>> GetPostCommentsAsync(
        Guid postId,
        Guid currentUserId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var isMember = await _groupRepository.IsUserMemberAsync(post.GroupId, currentUserId);
        if (!isMember)
            return Result.Forbidden("You must be an active member of this group to view comments.");

        var query = _commentRepository.Query()
            .Where(c => c.PostId == postId && c.ParentCommentId == null);

        var total = await query.CountAsync(cancellationToken);

        var comments = await query
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .OrderBy(c => c.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<CommentResponseDto>>(comments);

        // Populate IsLikedByCurrentUser for root comments and their replies
        var allCommentIds = new List<Guid>();
        foreach (var c in comments)
        {
            allCommentIds.Add(c.Id);
            allCommentIds.AddRange(c.Replies.Select(r => r.Id));
        }

        if (allCommentIds.Count > 0)
        {
            var likedCommentIds = await _commentLikeRepository.Query()
                .Where(cl => cl.UserId == currentUserId && allCommentIds.Contains(cl.CommentId))
                .Select(cl => cl.CommentId)
                .ToListAsync(cancellationToken);

            var likedSet = new HashSet<Guid>(likedCommentIds);

            void SetLikedStatus(CommentResponseDto dto)
            {
                dto.IsLikedByCurrentUser = likedSet.Contains(dto.Id);
                foreach (var reply in dto.Replies)
                {
                    SetLikedStatus(reply);
                }
            }

            foreach (var dto in dtos)
            {
                SetLikedStatus(dto);
            }
        }

        return Result.Success(new PagedResultDto<CommentResponseDto>
        {
            Items = dtos,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result<CommentResponseDto>> AddCommentAsync(
        Guid postId,
        Guid currentUserId,
        CreateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedContent = _xssSanitizer.Sanitize(request.Content);
        if (string.IsNullOrWhiteSpace(sanitizedContent))
            return Result.Invalid(new ValidationError("Comment content is required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync(sanitizedContent, cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var isMember = await _groupRepository.IsUserMemberAsync(post.GroupId, currentUserId);
        if (!isMember)
            return Result.Forbidden("You must be an active member of this group to comment.");

        Comment? parentComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentComment = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value);
            if (parentComment is null || parentComment.PostId != postId)
                return Result.Invalid(new ValidationError("Parent comment does not exist on this post."));
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = currentUserId,
            ParentCommentId = request.ParentCommentId,
            Content = sanitizedContent,
            CreatedAt = DateTime.UtcNow,
            LikesCount = 0,
            RepliesCount = 0
        };

        await _commentRepository.AddAsync(comment);

        // Synchronize denormalized counters
        post.CommentsCount++;
        _postRepository.Update(post);

        if (parentComment is not null)
        {
            parentComment.RepliesCount++;
            _commentRepository.Update(parentComment);
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("AddComment", "Comment", comment.Id, currentUserId, $"PostId:{postId}", cancellationToken);
        _logger.LogInformation("Comment {CommentId} added to post {PostId} by user {UserId}.", comment.Id, postId, currentUserId);

        var loadedComment = await _commentRepository.Query()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == comment.Id, cancellationToken);

        var dto = _mapper.Map<CommentResponseDto>(loadedComment ?? comment);
        dto.IsLikedByCurrentUser = false;

        // Broadcast real-time comment event to post viewers
        await _realTimeNotification.SendCommentCreatedToPostAsync(postId, dto, cancellationToken);

        // Notify post author or parent comment author if not current user
        if (parentComment is not null && parentComment.AuthorId != currentUserId)
        {
            await _notificationService.DispatchNotificationAsync(
                parentComment.AuthorId,
                NotificationType.NewReply,
                "Someone replied to your comment.",
                $"/posts/{postId}",
                cancellationToken);
        }
        else if (post.AuthorId != currentUserId)
        {
            await _notificationService.DispatchNotificationAsync(
                post.AuthorId,
                NotificationType.NewComment,
                "Someone commented on your post.",
                $"/posts/{postId}",
                cancellationToken);
        }

        return Result.Success(dto);
    }

    public async Task<Result<CommentResponseDto>> UpdateCommentAsync(
        Guid commentId,
        Guid currentUserId,
        UpdateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedContent = _xssSanitizer.Sanitize(request.Content);
        if (string.IsNullOrWhiteSpace(sanitizedContent))
            return Result.Invalid(new ValidationError("Comment content is required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync(sanitizedContent, cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        var comment = await _commentRepository.Query()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
            return Result.NotFound($"Comment with ID '{commentId}' was not found.");

        if (comment.AuthorId != currentUserId)
            return Result.Forbidden("Only the author can edit this comment.");

        comment.Content = sanitizedContent;
        comment.UpdatedAt = DateTime.UtcNow;

        _commentRepository.Update(comment);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("UpdateComment", "Comment", commentId, currentUserId, null, cancellationToken);
        _logger.LogInformation("Comment {CommentId} updated by user {UserId}.", commentId, currentUserId);

        var dto = _mapper.Map<CommentResponseDto>(comment);
        dto.IsLikedByCurrentUser = await _commentRepository.HasUserLikedCommentAsync(commentId, currentUserId);

        return Result.Success(dto);
    }

    public async Task<Result> DeleteCommentAsync(
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment is null)
            return Result.NotFound($"Comment with ID '{commentId}' was not found.");

        var post = await _postRepository.GetByIdAsync(comment.PostId);
        var userRole = post is not null ? await _groupRepository.GetUserRoleAsync(post.GroupId, currentUserId) : null;

        var canDelete = comment.AuthorId == currentUserId || userRole is GroupRole.Owner or GroupRole.Admin;
        if (!canDelete)
            return Result.Forbidden("You do not have permission to delete this comment.");

        _commentRepository.Delete(comment);

        // Synchronize denormalized counters
        if (post is not null && post.CommentsCount > 0)
        {
            post.CommentsCount--;
            _postRepository.Update(post);
        }

        if (comment.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.GetByIdAsync(comment.ParentCommentId.Value);
            if (parent is not null && parent.RepliesCount > 0)
            {
                parent.RepliesCount--;
                _commentRepository.Update(parent);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("DeleteComment", "Comment", commentId, currentUserId, null, cancellationToken);
        _logger.LogInformation("Comment {CommentId} deleted by user {UserId}.", commentId, currentUserId);

        return Result.Success();
    }

    public async Task<Result> LikeCommentAsync(
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment is null)
            return Result.NotFound($"Comment with ID '{commentId}' was not found.");

        var alreadyLiked = await _commentRepository.HasUserLikedCommentAsync(commentId, currentUserId);
        if (alreadyLiked)
            return Result.Conflict("You have already liked this comment.");

        var like = new CommentLike
        {
            CommentId = commentId,
            UserId = currentUserId,
            LikedAt = DateTime.UtcNow
        };

        await _commentRepository.AddLikeAsync(like);

        // Synchronize denormalized counter
        comment.LikesCount++;
        _commentRepository.Update(comment);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment {CommentId} liked by user {UserId}.", commentId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> UnlikeCommentAsync(
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment is null)
            return Result.NotFound($"Comment with ID '{commentId}' was not found.");

        var alreadyLiked = await _commentRepository.HasUserLikedCommentAsync(commentId, currentUserId);
        if (!alreadyLiked)
            return Result.NotFound("Like not found on this comment.");

        await _commentRepository.RemoveLikeAsync(commentId, currentUserId);

        // Synchronize denormalized counter
        if (comment.LikesCount > 0)
        {
            comment.LikesCount--;
            _commentRepository.Update(comment);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment {CommentId} unliked by user {UserId}.", commentId, currentUserId);
        return Result.Success();
    }
}
