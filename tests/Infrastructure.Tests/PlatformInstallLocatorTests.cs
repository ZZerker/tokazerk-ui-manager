using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class PlatformInstallLocatorTests
{
    private static void AddInstall(MockFileSystem fileSystem, string root, string? marker = "eden.dll")
    {
        fileSystem.AddFile(fileSystem.Path.Combine(root, "camelot.exe"), new MockFileData("exe"));
        fileSystem.AddDirectory(fileSystem.Path.Combine(root, "ui"));
        if (marker is not null)
        {
            fileSystem.AddFile(fileSystem.Path.Combine(root, marker), new MockFileData("dll"));
        }
    }

    [Fact]
    public async Task DetectAsync_FindsEdenLauncherInstall_FromConfigGameDir()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Games\Eden DAoC";
        AddInstall(fileSystem, root);
        fileSystem.AddFile(@"C:\AppData\eden-launcher\config.json", new MockFileData("""{"gameDir":"C:\\Games\\Eden DAoC","runtime":"wine"}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i is { Source: InstallSource.EdenLauncher, Server: ServerKind.Eden });
    }

    [Fact]
    public async Task DetectAsync_FindsBlackthornLauncherInstall_FromConfigGamePath()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Games\BT DAoC";
        AddInstall(fileSystem, root, marker: "btui_game_bridge.dll");
        fileSystem.AddFile(@"C:\AppData\bt-launcher\config.json", new MockFileData("""{"gamePath":"C:/Games/BT DAoC"}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i is { Source: InstallSource.BlackthornLauncher, Server: ServerKind.Blackthorn });
    }

    [Fact]
    public async Task DetectAsync_FindsLinuxWinePrefixInstall()
    {
        var fileSystem = new MockFileSystem();
        var root = "/home/user/.wine/drive_c/Program Files (x86)/Electronic Arts/Dark Age of Camelot";
        AddInstall(fileSystem, root);
        var environment = new LocatorEnvironment(null, "/home/user", null, null, IsWindows: false);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i.Source == InstallSource.PrefixScan);
    }

    [Fact]
    public async Task DetectAsync_FindsWindowsWellKnownFolder()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Program Files (x86)\Electronic Arts\Dark Age of Camelot";
        AddInstall(fileSystem, root);
        var environment = new LocatorEnvironment(null, null, @"C:\Program Files (x86)", @"C:\Program Files", IsWindows: true);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i.Source == InstallSource.WellKnown);
    }

    [Fact]
    public async Task DetectAsync_CollapsesDuplicateCandidates()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Program Files (x86)\Electronic Arts\Dark Age of Camelot";
        AddInstall(fileSystem, root);
        fileSystem.AddFile(@"C:\AppData\eden-launcher\config.json", new MockFileData($$"""{"gameDir":"{{root.Replace("\\", "\\\\")}}"}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, @"C:\Program Files (x86)", @"C:\Program Files", IsWindows: true);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Single(installs, i => i.GameRoot.Equals(root, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DetectAsync_FindsInstall_UnderLinuxStyleWinePrefixPath()
    {
        var fileSystem = new MockFileSystem();
        var root = "/home/u/.wine/drive_c/Program Files (x86)/Electronic Arts/Dark Age of Camelot";
        AddInstall(fileSystem, root);
        var environment = new LocatorEnvironment(null, "/home/u", null, null, IsWindows: false);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i.Source == InstallSource.PrefixScan);
    }

    [Fact]
    public async Task DetectAsync_FindsInstall_UnderEdenWinePrefixFromConfig()
    {
        var fileSystem = new MockFileSystem();
        var root = "/opt/pfx/drive_c/Program Files (x86)/Electronic Arts/Dark Age of Camelot";
        AddInstall(fileSystem, root);
        fileSystem.AddFile("/home/u/.config/eden-launcher/config.json", new MockFileData("""{"winePrefix":"/opt/pfx"}"""));
        var environment = new LocatorEnvironment(null, "/home/u", null, null, IsWindows: false);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Contains(installs, i => i.Source == InstallSource.PrefixScan);
    }

    [Fact]
    public async Task DetectAsync_IgnoresLauncherConfig_WhenRootIsArray()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/home/u/.config/eden-launcher/config.json", new MockFileData("""["not", "an", "object"]"""));
        var environment = new LocatorEnvironment(null, "/home/u", null, null, IsWindows: false);
        var locator = new PlatformInstallLocator(fileSystem, environment);

        var installs = await locator.DetectAsync(CancellationToken.None);

        Assert.Empty(installs);
    }

    [Fact]
    public void Validate_AcceptsGameRoot()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Games\Eden DAoC";
        AddInstall(fileSystem, root);
        var locator = new PlatformInstallLocator(fileSystem, new LocatorEnvironment(null, null, null, null, IsWindows: true));

        var install = locator.Validate(root);

        Assert.NotNull(install);
        Assert.Equal(root, install!.GameRoot);
    }

    [Fact]
    public void Validate_AcceptsCustomFolder()
    {
        var fileSystem = new MockFileSystem();
        var root = @"C:\Games\Eden DAoC";
        AddInstall(fileSystem, root);
        var locator = new PlatformInstallLocator(fileSystem, new LocatorEnvironment(null, null, null, null, IsWindows: true));

        var install = locator.Validate(fileSystem.Path.Combine(root, "ui", "custom"));

        Assert.NotNull(install);
        Assert.Equal(root, install!.GameRoot);
    }

    [Fact]
    public void Validate_ReturnsNull_ForInvalidFolder()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\NotAGame");
        var locator = new PlatformInstallLocator(fileSystem, new LocatorEnvironment(null, null, null, null, IsWindows: true));

        var install = locator.Validate(@"C:\NotAGame");

        Assert.Null(install);
    }
}
