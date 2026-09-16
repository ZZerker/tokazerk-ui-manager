using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class JsonToolConfigStoreTests
{
    [Fact]
    public async Task SaveUsesWindowsAppDataPath()
    {
        var fileSystem = new MockFileSystem();
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        await store.SaveAsync(ToolConfig.Default, CancellationToken.None);

        Assert.True(fileSystem.File.Exists(@"C:\AppData\TokaZerkUIConfig\config.json"));
        Assert.Empty(fileSystem.Directory.GetFiles(@"C:\AppData\TokaZerkUIConfig", "*.tmp"));
    }

    [Fact]
    public async Task SaveUsesLinuxConfigPath()
    {
        var fileSystem = new MockFileSystem();
        var environment = new LocatorEnvironment(null, "/home/user", null, null, IsWindows: false);
        var store = new JsonToolConfigStore(fileSystem, environment);

        await store.SaveAsync(ToolConfig.Default, CancellationToken.None);

        Assert.True(fileSystem.File.Exists("/home/user/.config/TokaZerkUIConfig/config.json"));
    }

    [Fact]
    public async Task LoadReturnsDefaultWhenFileIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(ToolConfig.Default, config);
    }

    [Fact]
    public async Task SaveThenLoadRoundTrips()
    {
        var fileSystem = new MockFileSystem();
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);
        var expected = new ToolConfig(UpdateChannel.Beta, CheckForUpdatesOnStart: false);

        await store.SaveAsync(expected, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task LoadReturnsDefaultWhenJsonIsMalformed()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\AppData\TokaZerkUIConfig\config.json", new MockFileData("{ not json"));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(ToolConfig.Default, config);
    }

    [Fact]
    public async Task LoadReturnsDefaultWhenJsonIsNull()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\AppData\TokaZerkUIConfig\config.json", new MockFileData("null"));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(ToolConfig.Default, config);
    }

    [Fact]
    public async Task LoadNormalizesUnknownNumericUpdateChannel()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            @"C:\AppData\TokaZerkUIConfig\config.json",
            new MockFileData("""{"updateChannel":42,"checkForUpdatesOnStart":false}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(UpdateChannel.Stable, config.UpdateChannel);
        Assert.False(config.CheckForUpdatesOnStart);
    }

    [Fact]
    public async Task LoadNormalizesUnknownStringUpdateChannel()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            @"C:\AppData\TokaZerkUIConfig\config.json",
            new MockFileData("""{"updateChannel":"Nightly","checkForUpdatesOnStart":false}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(UpdateChannel.Stable, config.UpdateChannel);
        Assert.False(config.CheckForUpdatesOnStart);
    }

    [Fact]
    public async Task LoadNormalizesNullUpdateChannel()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            @"C:\AppData\TokaZerkUIConfig\config.json",
            new MockFileData("""{"updateChannel":null,"checkForUpdatesOnStart":false}"""));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(UpdateChannel.Stable, config.UpdateChannel);
        Assert.False(config.CheckForUpdatesOnStart);
    }

    [Fact]
    public async Task MissingBasePathLoadsDefaultAndRejectsSave()
    {
        var fileSystem = new MockFileSystem();
        var environment = new LocatorEnvironment(null, null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);

        var config = await store.LoadAsync(CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.SaveAsync(ToolConfig.Default, CancellationToken.None));

        Assert.Equal(ToolConfig.Default, config);
        Assert.Contains("user configuration directory", exception.Message);
    }

    [Fact]
    public async Task CancelledSavePreservesExistingConfig()
    {
        var fileSystem = new MockFileSystem();
        var path = @"C:\AppData\TokaZerkUIConfig\config.json";
        var original = """{"updateChannel":"Beta","checkForUpdatesOnStart":false}""";
        fileSystem.AddFile(path, new MockFileData(original));
        var environment = new LocatorEnvironment(@"C:\AppData", null, null, null, IsWindows: true);
        var store = new JsonToolConfigStore(fileSystem, environment);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SaveAsync(ToolConfig.Default, cancellation.Token));

        Assert.Equal(original, fileSystem.File.ReadAllText(path));
        Assert.Empty(fileSystem.Directory.GetFiles(@"C:\AppData\TokaZerkUIConfig", "*.tmp"));
    }
}
