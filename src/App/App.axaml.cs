using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TokaZerkUIConfig.App.ViewModels;
using TokaZerkUIConfig.App.Views;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain.Ports;
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
        Services.GetRequiredService<ISelfUpdater>().CleanupOnStart();

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            desktop.MainWindow.Opened += async (_, _) => await viewModel.InitializeAsync(CancellationToken.None);

            desktop.Exit += (_, _) => (Services as IDisposable)?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
