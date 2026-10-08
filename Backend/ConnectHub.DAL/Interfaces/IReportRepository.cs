using ConnectHub.Models.Entities;

namespace ConnectHub.DAL.Interfaces;

public interface IReportRepository : IGenericRepository<Report>
{
    Task<Report?> GetWithDetailsAsync(Guid reportId);
}
