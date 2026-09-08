using Microsoft.Extensions.DependencyInjection;

namespace WayPoint.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
