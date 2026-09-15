using Microsoft.Extensions.DependencyInjection;
using TokaZerkUIConfig.App.ViewModels;

namespace TokaZerkUIConfig.App;

public static class DependencyInjection
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<InstallViewModel>();
        services.AddTransient<MapsViewModel>();
        services.AddTransient<FontsViewModel>();
        services.AddTransient<WindowsViewModel>();
        services.AddTransient<UpdatesViewModel>();
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
