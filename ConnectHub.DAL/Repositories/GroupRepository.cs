using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.DAL.Repositories;

public class GroupRepository : GenericRepository<Group>, IGroupRepository
{
    public GroupRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Group?> GetWithDetailsAsync(Guid groupId)
    {
        return await _context.Groups
            .Include(g => g.Category)
            .Include(g => g.Tags)
            .Include(g => g.CreatedBy)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<bool> IsUserMemberAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);
    }

    public async Task<GroupRole?> GetUserRoleAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive)
            .Select(gm => (GroupRole?)gm.Role)
            .FirstOrDefaultAsync();
    }
}
