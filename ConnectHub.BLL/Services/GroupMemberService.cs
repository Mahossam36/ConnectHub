using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class GroupMemberService : IGroupMemberService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGenericRepository<GroupMember> _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ILogger<GroupMemberService> _logger;

    public GroupMemberService(
        IGroupRepository groupRepository,
        IGenericRepository<GroupMember> groupMemberRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        ILogger<GroupMemberService> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<GroupMemberResponseDto>>> GetMembersAsync(
        Guid groupId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var groupExists = await _groupRepository.ExistsAsync(g => g.Id == groupId && g.IsActive);
        if (!groupExists)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var query = _groupMemberRepository.Query()
            .Where(gm => gm.GroupId == groupId && gm.IsActive);

        var total = await query.CountAsync(cancellationToken);

        var members = await query
            .Include(gm => gm.User)
            .OrderBy(gm => gm.Role)
            .ThenBy(gm => gm.JoinedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GroupMemberResponseDto>>(members);

        return Result.Success(new PagedResultDto<GroupMemberResponseDto>
        {
            Items = dtos,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result> JoinGroupAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var existingMembership = await _groupMemberRepository.Query()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId, cancellationToken);

        if (existingMembership is not null && existingMembership.IsActive)
            return Result.Conflict("You are already a member of this group.");

        if (existingMembership is not null)
        {
            existingMembership.IsActive = true;
            existingMembership.JoinedAt = DateTime.UtcNow;
            existingMembership.Role = GroupRole.Member;
            _groupMemberRepository.Update(existingMembership);
        }
        else
        {
            var membership = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = currentUserId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _groupMemberRepository.AddAsync(membership);
        }

        // Synchronize denormalized counter
        group.CountMembers++;
        _groupRepository.Update(group);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("JoinGroup", "GroupMember", groupId, currentUserId, null, cancellationToken);
        _logger.LogInformation("User {UserId} joined group {GroupId}.", currentUserId, groupId);

        return Result.Success();
    }

    public async Task<Result> LeaveGroupAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var membership = await _groupMemberRepository.Query()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.IsActive, cancellationToken);

        if (membership is null)
            return Result.NotFound("You are not an active member of this group.");

        if (membership.Role == GroupRole.Owner)
            return Result.Conflict("The group owner cannot leave the group. Transfer ownership or delete the group.");

        membership.IsActive = false;
        _groupMemberRepository.Update(membership);

        // Synchronize denormalized counter
        if (group.CountMembers > 0)
            group.CountMembers--;
        _groupRepository.Update(group);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("LeaveGroup", "GroupMember", groupId, currentUserId, null, cancellationToken);
        _logger.LogInformation("User {UserId} left group {GroupId}.", currentUserId, groupId);

        return Result.Success();
    }

    public async Task<Result> ChangeMemberRoleAsync(
        Guid groupId,
        Guid targetUserId,
        Guid currentUserId,
        ChangeMemberRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserRole = await _groupRepository.GetUserRoleAsync(groupId, currentUserId);
        if (currentUserRole is not (GroupRole.Owner or GroupRole.Admin))
            return Result.Forbidden("Only group owners and admins can change member roles.");

        var targetMembership = await _groupMemberRepository.Query()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId && gm.IsActive, cancellationToken);

        if (targetMembership is null)
            return Result.NotFound("Target member was not found in this group.");

        if (targetMembership.Role == GroupRole.Owner)
            return Result.Forbidden("Cannot change the role of the group owner.");

        if (currentUserRole == GroupRole.Admin && (targetMembership.Role == GroupRole.Admin || request.Role == GroupRole.Owner))
            return Result.Forbidden("Admins cannot change the role of other admins or promote members to owner.");

        targetMembership.Role = request.Role;
        _groupMemberRepository.Update(targetMembership);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("ChangeMemberRole", "GroupMember", targetMembership.Id, currentUserId, $"NewRole:{request.Role}", cancellationToken);
        _logger.LogInformation("Role of user {TargetUserId} in group {GroupId} changed to {Role} by {CurrentUserId}.", targetUserId, groupId, request.Role, currentUserId);

        return Result.Success();
    }

    public async Task<Result> RemoveMemberAsync(
        Guid groupId,
        Guid targetUserId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group is null || !group.IsActive)
            return Result.NotFound($"Group with ID '{groupId}' was not found.");

        var currentUserRole = await _groupRepository.GetUserRoleAsync(groupId, currentUserId);
        if (currentUserRole is not (GroupRole.Owner or GroupRole.Admin))
            return Result.Forbidden("Only group owners and admins can remove members.");

        var targetMembership = await _groupMemberRepository.Query()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId && gm.IsActive, cancellationToken);

        if (targetMembership is null)
            return Result.NotFound("Target member was not found in this group.");

        if (targetMembership.Role == GroupRole.Owner)
            return Result.Forbidden("Cannot remove the group owner.");

        if (currentUserRole == GroupRole.Admin && targetMembership.Role == GroupRole.Admin)
            return Result.Forbidden("Admins cannot remove other admins.");

        targetMembership.IsActive = false;
        _groupMemberRepository.Update(targetMembership);

        // Synchronize denormalized counter
        if (group.CountMembers > 0)
            group.CountMembers--;
        _groupRepository.Update(group);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("RemoveMember", "GroupMember", targetMembership.Id, currentUserId, $"RemovedUser:{targetUserId}", cancellationToken);
        _logger.LogInformation("User {TargetUserId} removed from group {GroupId} by user {CurrentUserId}.", targetUserId, groupId, currentUserId);

        return Result.Success();
    }
}
