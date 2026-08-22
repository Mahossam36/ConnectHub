using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.DAL.Repositories;

public class ReportRepository : GenericRepository<Report>, IReportRepository
{
    public ReportRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Report?> GetWithDetailsAsync(Guid reportId)
    {
        return await _context.Reports
            .Include(r => r.ReportedBy)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }
}
