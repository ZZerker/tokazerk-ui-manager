using System.Runtime.InteropServices;

namespace TokaZerkUIConfig.Infrastructure;

public sealed record LocatorEnvironment(string? AppData, string? Home, string? ProgramFilesX86, string? ProgramFiles, bool IsWindows)
{
    public static LocatorEnvironment Current() => new(
        Environment.GetEnvironmentVariable("APPDATA"),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
        Environment.GetEnvironmentVariable("ProgramFiles"),
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
}
