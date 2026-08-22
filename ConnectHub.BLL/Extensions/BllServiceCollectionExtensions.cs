using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectHub.BLL.Extensions;

public static class BllServiceCollectionExtensions
{
    public static IServiceCollection AddBllServices(this IServiceCollection services)
    {
        // Automatically register all FluentValidation validators in this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Note: Concrete service implementations (e.g. AuthService, GroupService, etc.)
        // will be registered here as they are created in the Services folder.

        return services;
    }
}
