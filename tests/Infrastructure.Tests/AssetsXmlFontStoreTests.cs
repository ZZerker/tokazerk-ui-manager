using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class AssetsXmlFontStoreTests
{
    private static readonly string SampleAssetsXml = Path.Combine(AppContext.BaseDirectory, "TestData", "assets.xml");

    private static MockFileSystem CreateFileSystem(out byte[] originalBytes)
    {
        originalBytes = File.ReadAllBytes(SampleAssetsXml);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom");
        fileSystem.AddFile("custom/assets.xml", new MockFileData(originalBytes));
        return fileSystem;
    }

    [Fact]
    public async Task ReadAsync_ReturnsDefaults()
    {
        var fileSystem = CreateFileSystem(out _);
        var store = new AssetsXmlFontStore(fileSystem);

        var settings = await store.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(FontSettings.Default, settings);
    }

    [Fact]
    public async Task WriteAsync_ThenReadAsync_ReturnsWrittenValue()
    {
        var fileSystem = CreateFileSystem(out _);
        var store = new AssetsXmlFontStore(fileSystem);

        await store.WriteAsync("custom", FontSettings.Default.With(FontTier.Large, 14), CancellationToken.None);
        var settings = await store.ReadAsync("custom", CancellationToken.None);

        Assert.Equal(14, settings.Large);
    }

    [Fact]
    public async Task WriteAsync_DefaultsAfterChange_IsByteIdenticalToOriginal()
    {
        var fileSystem = CreateFileSystem(out var originalBytes);
        var store = new AssetsXmlFontStore(fileSystem);

        await store.WriteAsync("custom", FontSettings.Default.With(FontTier.Large, 14), CancellationToken.None);
        await store.WriteAsync("custom", FontSettings.Default, CancellationToken.None);

        var resultBytes = fileSystem.GetFile("custom/assets.xml").Contents;
        Assert.Equal(originalBytes, resultBytes);
    }
}
