using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TaskApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                Assembly.GetExecutingAssembly());
        });

        return services;
    }
}