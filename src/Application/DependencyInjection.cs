using Microsoft.Extensions.DependencyInjection;

namespace TokaZerkUIConfig.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<DetectInstalls>();
        services.AddTransient<ValidateInstall>();
        services.AddTransient<LoadCurrentState>();
        services.AddTransient<ApplyFontSettings>();
        services.AddTransient<ApplyVariant>();
        services.AddTransient<ResetAll>();

        return services;
    }
}
