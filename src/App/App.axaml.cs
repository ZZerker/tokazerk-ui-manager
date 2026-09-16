using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TokaZerkUIConfig.App.ViewModels;
using TokaZerkUIConfig.App.Views;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Infrastructure;

namespace TokaZerkUIConfig.App;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddViewModels();
        Services = services.BuildServiceProvider();

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.Exit += (_, _) => (Services as IDisposable)?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
