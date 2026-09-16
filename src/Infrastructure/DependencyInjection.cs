using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(LocatorEnvironment.Current());
        // One HttpClient per process avoids socket exhaustion from repeated instantiation.
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IReleaseSource, GitHubReleaseSource>();

        services.AddSingleton<IFontDefinitionStore, AssetsXmlFontStore>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<IBackupStore, FolderBackupStore>();
        services.AddSingleton<IVariantStore, FileVariantStore>();
        services.AddSingleton<IInstallLocator, PlatformInstallLocator>();
        services.AddSingleton<IInstalledUiReader, TokazerkJsonReader>();
        services.AddSingleton<IUiArchiveStore, ZipUiArchiveStore>();
        services.AddSingleton<IUiPreviewRenderer, ForgePreviewRenderer>();
        services.AddSingleton<IMapThumbnailSource, DdsThumbnailSource>();

        return services;
    }
}
