using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.DTOs.Users;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.BLL.Interfaces.Storage;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Tag> _tagRepository;
    private readonly IGenericRepository<GroupMember> _groupMemberRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly IContentModerationService _contentModeration;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<GroupService> _logger;

    private const string GroupCachePrefix = "group_detail_";
    private const string GroupCoverImageFolder = "uploads/groups";

    public GroupService(
        IGroupRepository groupRepository,
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Tag> tagRepository,
        IGenericRepository<GroupMember> groupMemberRepository,
        IGenericRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        IXssSanitizerService xssSanitizer,
        IContentModerationService contentModeration,
        IFileStorageService fileStorageService,
        IMemoryCache memoryCache,
        ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _xssSanitizer = xssSanitizer;
        _contentModeration = contentModeration;
        _fileStorageService = fileStorageService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<GroupSummaryResponseDto>>> BrowseGroupsAsync(
        Guid? currentUserId,
        Guid? categoryId,
        Guid? tagId,
        string? search,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _groupRepository.Query()
            .Where(g => g.IsActive);

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (tagId.HasValue)
            query = query.Where(g => g.Tags.Any(t => t.Id == tagId.Value));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(g => g.Name.Contains(term) || (g.Description != null && g.Description.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var groups = await query
            .Include(g => g.Category)
            .Include(g => g.Tags)
            .OrderByDescending(g => g.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GroupSummaryResponseDto>>(groups);

        if (currentUserId.HasValue && groups.Count > 0)
        {
            var groupIds = groups.Select(g => g.Id).ToList();
            var userMemberships = await _groupMemberRepository.Query()
                .Where(gm => gm.UserId == currentUserId.Value && groupIds.Contains(gm.GroupId) && gm.IsActive)
                .ToDictionaryAsync(gm => gm.GroupId, gm => gm.Role, cancellationToken);

            foreach (var dto in dtos)
            {
                if (userMemberships.TryGetValue(dto.Id, out var role))
                    dto.CurrentUserRole = role;
            }
        }

        return Result.Success(new PagedResultDto<GroupSummaryResponseDto>
        {
            Items = dtos,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result<GroupDetailResponseDto>> GetGroupByIdAsync(
        Guid groupId,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{GroupCachePrefix}{groupId}";
        if (!_memoryCache.TryGetValue(cacheKey, out GroupDetailResponseDto? cachedDto) || cachedDto is null)
        {
            var group = await _groupRepository.GetWithDetailsAsync(groupId);
            if (group is null || !group.IsActive)
                return Result.NotFound($"Group with ID '{groupId}' was not found.");

            cachedDto = _mapper.Map<GroupDetailResponseDto>(group);
            _memoryCache.Set(cacheKey, cachedDto, TimeSpan.FromMinutes(10));
        }

        // Clone and compute dynamic per-user role
        var dto = new GroupDetailResponseDto
        {
            Id = cachedDto.Id,
            Name = cachedDto.Name,
            Description = cachedDto.Description,
            CoverImageUrl = cachedDto.CoverImageUrl,
            Category = cachedDto.Category,
            Tags = cachedDto.Tags,
            MemberCount = cachedDto.MemberCount,
            CreatedBy = cachedDto.CreatedBy,
            CreatedAt = cachedDto.CreatedAt
        };

        if (currentUserId.HasValue)
        {
            dto.CurrentUserRole = await _groupRepository.GetUserRoleAsync(groupId, currentUserId.Value);
        }

        return Result.Success(dto);
    }

    public async Task<Result<GroupDetailResponseDto>> CreateGroupAsync(
        Guid currentUserId,
        CreateGroupRequestDto request,
        Stream? coverImageStream,
        string? coverImageFileName,
        CancellationToken cancellationToken = default)
    {
        var sanitizedName = _xssSanitizer.Sanitize(request.Name);
        var sanitizedDescription = _xssSanitizer.Sanitize(request.Description);

        if (string.IsNullOrWhiteSpace(sanitizedName))
            return Result.Invalid(new ValidationError("Group name is required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync($"{sanitizedName} {sanitizedDescription}", cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        var categoryExists = await _categoryRepository.ExistsAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return Result.Invalid(new ValidationError("Specified category does not exist."));

        var user = await _userRepository.GetByIdAsync(currentUserId);
        if (user is null)
            return Result.NotFound("User profile not found.");

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = sanitizedName,
            Description = string.IsNullOrWhiteSpace(sanitizedDescription) ? null : sanitizedDescription,
            CategoryId = request.CategoryId,
            CreatedById = currentUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            CountMembers = 1,
            PostCount = 0
        };

        if (coverImageStream is not null)
        {
            var extension = Path.GetExtension(coverImageFileName);
            group.CoverImagePath = await _fileStorageService.SaveFileAsync(
                coverImageStream,
                $"{group.Id}{extension}",
                GroupCoverImageFolder);
        }

        if (request.TagIds.Count > 0)
        {
            var tags = await _tagRepository.Query()
                .AsTracking()
                .Where(t => request.TagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            if (tags.Count != request.TagIds.Distinct().Count())
                return Result.Invalid(new ValidationError("One or more specified tags do not exist."));

            group.Tags = tags;
        }

        var ownerMembership = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = currentUserId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _groupRepository.AddAsync(group);
        await _groupMemberRepository.AddAsync(ownerMembership);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("CreateGroup", "Group", group.Id, currentUserId, null, cancellationToken);
        _logger.LogInformation("Group {GroupId} created by user {UserId}.", group.Id, currentUserId);

        var detailedGroup = await _groupRepository.GetWithDetailsAsync(group.Id);
        var dto = _mapper.Map<GroupDetailResponseDto>(detailedGroup ?? group);
        dto.CurrentUserRole = GroupRole.Owner;

        return Result.Success(dto);
    }

    public async Task<Result<GroupDetailResponseDto>> UpdateGroupAsync(
        Guid groupId,
        Guid currentUserId,
        UpdateGroupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetWithDetailsAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var userRole = await _groupRepository.GetUserRoleAsync(groupId, currentUserId);
        if (userRole is not (GroupRole.Owner or GroupRole.Admin))
            return Result.Forbidden("Only group owners and admins can update group settings.");

        var sanitizedName = _xssSanitizer.Sanitize(request.Name);
        var sanitizedDescription = _xssSanitizer.Sanitize(request.Description);

        if (!string.IsNullOrWhiteSpace(sanitizedName))
        {
            var moderationResult = await _contentModeration.IsContentSafeAsync($"{sanitizedName} {sanitizedDescription}", cancellationToken);
            if (!moderationResult.IsSuccess)
                return Result.Invalid(moderationResult.ValidationErrors);

            group.Name = sanitizedName;
        }

        if (request.CategoryId != Guid.Empty && request.CategoryId != group.CategoryId)
        {
            var categoryExists = await _categoryRepository.ExistsAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return Result.Invalid(new ValidationError("Specified category does not exist."));
            group.CategoryId = request.CategoryId;
        }

        var tags = await _tagRepository.Query()
            .Where(t => request.TagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
        if (tags.Count != request.TagIds.Distinct().Count())
            return Result.Invalid(new ValidationError("One or more specified tags do not exist."));
        group.Tags = tags;

        group.Description = string.IsNullOrWhiteSpace(sanitizedDescription) ? null : sanitizedDescription;
        if (request.CoverImageUrl is not null)
            group.CoverImagePath = request.CoverImageUrl;

        group.UpdatedAt = DateTime.UtcNow;

        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        _memoryCache.Remove($"{GroupCachePrefix}{groupId}");

        await _auditService.LogAsync("UpdateGroup", "Group", group.Id, currentUserId, null, cancellationToken);
        _logger.LogInformation("Group {GroupId} updated by user {UserId}.", groupId, currentUserId);

        var dto = _mapper.Map<GroupDetailResponseDto>(group);
        dto.CurrentUserRole = userRole;

        return Result.Success(dto);
    }

    public async Task<Result> DeleteGroupAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var userRole = await _groupRepository.GetUserRoleAsync(groupId, currentUserId);
        if (userRole is not GroupRole.Owner)
            return Result.Forbidden("Only the group owner can delete the group.");

        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;

        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        _memoryCache.Remove($"{GroupCachePrefix}{groupId}");

        await _auditService.LogAsync("DeleteGroup", "Group", groupId, currentUserId, null, cancellationToken);
        _logger.LogInformation("Group {GroupId} soft-deleted by owner {UserId}.", groupId, currentUserId);

        return Result.Success();
    }
}
