using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App;

internal static class AppVersion
{
    public static SemVer Current { get; } = FromAssembly();

    public static string Rid => System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

    private static SemVer FromAssembly()
    {
        var version = typeof(AppVersion).Assembly.GetName().Version;
        return version is null ? new SemVer(0, 0, 0) : new SemVer(version.Major, version.Minor, version.Build);
    }
}
