using System.Reflection;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.BLL.Interfaces.Storage;
using ConnectHub.BLL.Mappers;
using ConnectHub.BLL.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectHub.BLL.Extensions;

public static class BllServiceCollectionExtensions
{
    public static IServiceCollection AddBllServices(this IServiceCollection services)
    {
        // 1. Memory Cache
        services.AddMemoryCache();

        // 2. Automatically register all FluentValidation validators in this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // 3. Register AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ConnectHubProfile>();
        });

        // 4. Register Utility and Infrastructure Services
        services.AddSingleton<IXssSanitizerService, XssSanitizerService>();
        services.AddHttpClient<IContentModerationService, ContentModerationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IRealTimeNotificationService, NullRealTimeNotificationService>();

        // 5. Register Domain BLL Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupMemberService, GroupMemberService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        return services;
    }
}
