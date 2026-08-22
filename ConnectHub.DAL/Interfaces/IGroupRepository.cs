using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;

namespace ConnectHub.DAL.Interfaces;

public interface IGroupRepository : IGenericRepository<Group>
{
    Task<Group?> GetWithDetailsAsync(Guid groupId);
    Task<bool> IsUserMemberAsync(Guid groupId, Guid userId);
    Task<GroupRole?> GetUserRoleAsync(Guid groupId, Guid userId);
}
