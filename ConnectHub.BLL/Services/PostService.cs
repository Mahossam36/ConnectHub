using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Posts;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IGenericRepository<PostLike> _postLikeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly IContentModerationService _contentModeration;
    private readonly IRealTimeNotificationService _realTimeNotification;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        IAttachmentRepository attachmentRepository,
        IGenericRepository<PostLike> postLikeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        IXssSanitizerService xssSanitizer,
        IContentModerationService contentModeration,
        IRealTimeNotificationService realTimeNotification,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _attachmentRepository = attachmentRepository;
        _postLikeRepository = postLikeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _xssSanitizer = xssSanitizer;
        _contentModeration = contentModeration;
        _realTimeNotification = realTimeNotification;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<PostResponseDto>>> GetGroupFeedAsync(
        Guid groupId,
        Guid currentUserId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var isMember = await _groupRepository.IsUserMemberAsync(groupId, currentUserId);
        if (!isMember)
            return Result.Forbidden("You must be an active member of this group to view its feed.");

        var query = _postRepository.Query()
            .Where(p => p.GroupId == groupId);

        var total = await query.CountAsync(cancellationToken);

        var posts = await query
            .Include(p => p.Author)
            .Include(p => p.Attachments)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<PostResponseDto>>(posts);

        if (posts.Count > 0)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            var likedPostIds = await _postLikeRepository.Query()
                .Where(pl => pl.UserId == currentUserId && postIds.Contains(pl.PostId))
                .Select(pl => pl.PostId)
                .ToListAsync(cancellationToken);

            var likedSet = new HashSet<Guid>(likedPostIds);
            foreach (var dto in dtos)
            {
                dto.IsLikedByCurrentUser = likedSet.Contains(dto.Id);
            }
        }

        return Result.Success(new PagedResultDto<PostResponseDto>
        {
            Items = dtos,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result<PostResponseDto>> GetPostByIdAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetWithDetailsAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var isMember = await _groupRepository.IsUserMemberAsync(post.GroupId, currentUserId);
        if (!isMember)
            return Result.Forbidden("You must be an active member of the group to view this post.");

        var dto = _mapper.Map<PostResponseDto>(post);
        dto.IsLikedByCurrentUser = await _postRepository.HasUserLikedPostAsync(postId, currentUserId);

        return Result.Success(dto);
    }

    public async Task<Result<PostResponseDto>> CreatePostAsync(
        Guid groupId,
        Guid currentUserId,
        CreatePostRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedContent = _xssSanitizer.Sanitize(request.Content);
        if (string.IsNullOrWhiteSpace(sanitizedContent))
            return Result.Invalid(new ValidationError("Post content is required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync(sanitizedContent, cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        var isMember = await _groupRepository.IsUserMemberAsync(groupId, currentUserId);
        if (!isMember)
            return Result.Forbidden("You must be an active member of this group to create a post.");

        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var post = new Post
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AuthorId = currentUserId,
            Content = sanitizedContent,
            CreatedAt = DateTime.UtcNow,
            IsPinned = false,
            LikesCount = 0,
            CommentsCount = 0,
            AttachmentsCount = 0
        };

        if (request.AttachmentIds.Count > 0)
        {
            var attachments = await _attachmentRepository.Query()
                .Where(a => request.AttachmentIds.Contains(a.Id) && a.UploadedById == currentUserId && a.PostId == null)
                .ToListAsync(cancellationToken);

            foreach (var att in attachments)
            {
                att.PostId = post.Id;
            }
            post.Attachments = attachments;
            post.AttachmentsCount = attachments.Count;
        }

        await _postRepository.AddAsync(post);

        // Synchronize denormalized group counter
        group.PostCount++;
        _groupRepository.Update(group);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("CreatePost", "Post", post.Id, currentUserId, $"GroupId:{groupId}", cancellationToken);
        _logger.LogInformation("Post {PostId} created in group {GroupId} by user {UserId}.", post.Id, groupId, currentUserId);

        var detailedPost = await _postRepository.GetWithDetailsAsync(post.Id);
        var dto = _mapper.Map<PostResponseDto>(detailedPost ?? post);
        dto.IsLikedByCurrentUser = false;

        // Broadcast real-time event to group members
        await _realTimeNotification.SendPostCreatedToGroupAsync(groupId, dto, cancellationToken);

        return Result.Success(dto);
    }

    public async Task<Result<PostResponseDto>> UpdatePostAsync(
        Guid postId,
        Guid currentUserId,
        UpdatePostRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedContent = _xssSanitizer.Sanitize(request.Content);
        if (string.IsNullOrWhiteSpace(sanitizedContent))
            return Result.Invalid(new ValidationError("Post content is required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync(sanitizedContent, cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        var post = await _postRepository.GetWithDetailsAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        if (post.AuthorId != currentUserId)
            return Result.Forbidden("Only the author can edit this post.");

        post.Content = sanitizedContent;
        post.UpdatedAt = DateTime.UtcNow;

        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("UpdatePost", "Post", postId, currentUserId, null, cancellationToken);
        _logger.LogInformation("Post {PostId} updated by user {UserId}.", postId, currentUserId);

        var dto = _mapper.Map<PostResponseDto>(post);
        dto.IsLikedByCurrentUser = await _postRepository.HasUserLikedPostAsync(postId, currentUserId);

        return Result.Success(dto);
    }

    public async Task<Result> DeletePostAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var group = await _groupRepository.GetByIdAsync(post.GroupId);
        var userRole = await _groupRepository.GetUserRoleAsync(post.GroupId, currentUserId);

        var canDelete = post.AuthorId == currentUserId || userRole is GroupRole.Owner or GroupRole.Admin;
        if (!canDelete)
            return Result.Forbidden("You do not have permission to delete this post.");

        _postRepository.Delete(post);

        // Synchronize denormalized group counter
        if (group is not null && group.PostCount > 0)
        {
            group.PostCount--;
            _groupRepository.Update(group);
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("DeletePost", "Post", postId, currentUserId, null, cancellationToken);
        _logger.LogInformation("Post {PostId} deleted by user {UserId}.", postId, currentUserId);

        return Result.Success();
    }

    public async Task<Result> PinPostAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var userRole = await _groupRepository.GetUserRoleAsync(post.GroupId, currentUserId);
        if (userRole is not (GroupRole.Owner or GroupRole.Admin))
            return Result.Forbidden("Only group owners and admins can pin posts.");

        post.IsPinned = true;
        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} pinned by user {UserId}.", postId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> UnpinPostAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var userRole = await _groupRepository.GetUserRoleAsync(post.GroupId, currentUserId);
        if (userRole is not (GroupRole.Owner or GroupRole.Admin))
            return Result.Forbidden("Only group owners and admins can unpin posts.");

        post.IsPinned = false;
        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} unpinned by user {UserId}.", postId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> LikePostAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var alreadyLiked = await _postRepository.HasUserLikedPostAsync(postId, currentUserId);
        if (alreadyLiked)
            return Result.Conflict("You have already liked this post.");

        var like = new PostLike
        {
            PostId = postId,
            UserId = currentUserId,
            LikedAt = DateTime.UtcNow
        };

        await _postRepository.AddLikeAsync(like);

        // Synchronize denormalized counter
        post.LikesCount++;
        _postRepository.Update(post);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} liked by user {UserId}.", postId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> UnlikePostAsync(
        Guid postId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post is null)
            return Result.NotFound($"Post with ID '{postId}' was not found.");

        var alreadyLiked = await _postRepository.HasUserLikedPostAsync(postId, currentUserId);
        if (!alreadyLiked)
            return Result.NotFound("Like not found on this post.");

        await _postRepository.RemoveLikeAsync(postId, currentUserId);

        // Synchronize denormalized counter
        if (post.LikesCount > 0)
        {
            post.LikesCount--;
            _postRepository.Update(post);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} unliked by user {UserId}.", postId, currentUserId);
        return Result.Success();
    }
}
