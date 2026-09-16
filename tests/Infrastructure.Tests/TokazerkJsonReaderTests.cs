using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class TokazerkJsonReaderTests
{
    [Fact]
    public async Task ReadAsyncReturnsTokaZerkForValidMarker()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom");
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData("""{"name":"TokaZerkUI","version":"1.0.12","built":"2026-09-15"}"""));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(InstalledUiKind.TokaZerk, installedUi.Kind);
        Assert.Equal(new SemVer(1, 0, 12), installedUi.Version);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForMarkerlessDirectoryWithContent()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/assets.xml", new MockFileData("content"));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsNoneForMissingDirectory()
    {
        var reader = new TokazerkJsonReader(new MockFileSystem());

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.None, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsNoneForEmptyDirectory()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom");
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.None, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForMalformedMarker()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("custom/tokazerk.json", new MockFileData("{ invalid"));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForWrongMarkerName()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData("""{"name":"AnotherUI","version":"1.0.12"}"""));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForMissingBuiltDate()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData("""{"name":"TokaZerkUI","version":"1.0.12"}"""));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForInvalidBuiltDate()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData("""{"name":"TokaZerkUI","version":"1.0.12","built":"2026-02-30"}"""));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }

    [Fact]
    public async Task ReadAsyncReturnsOtherForVersionWithTrailingGarbage()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData("""{"name":"TokaZerkUI","version":"1.0.12-preview","built":"2026-09-15"}"""));
        var reader = new TokazerkJsonReader(fileSystem);

        var installedUi = await reader.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(new InstalledUi(InstalledUiKind.Other, null), installedUi);
    }
}
