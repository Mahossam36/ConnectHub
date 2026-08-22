using ConnectHub.DAL.Context;
using ConnectHub.DAL.Identity;
using ConnectHub.DAL.Interfaces;
using ConnectHub.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectHub.DAL.Extensions;

public static class DalServiceCollectionExtensions
{
    public static IServiceCollection AddDalServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        // 1. Register DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 2. Configure ASP.NET Core Identity with ApplicationUser & Guid PKs
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        // 3. Register Generic Repository, Specific Repositories, and Unit of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
