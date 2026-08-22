using ConnectHub.Models.Entities;

namespace ConnectHub.DAL.Interfaces;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
