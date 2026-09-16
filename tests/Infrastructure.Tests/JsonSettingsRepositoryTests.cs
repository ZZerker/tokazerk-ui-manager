using System.IO.Abstractions.TestingHelpers;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class JsonSettingsRepositoryTests
{
    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFileMissing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom");
        var repository = new JsonSettingsRepository(fileSystem);

        var settings = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.Null(settings);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom");
        var repository = new JsonSettingsRepository(fileSystem);
        var settings = new UiSettings(
            FontSettings.Default.With(FontTier.Large, 14),
            new VariantSelection(MapSize: "large"));

        await repository.SaveAsync("custom", settings, CancellationToken.None);
        var loaded = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFontsOrVariantsMissing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom/tokazerk_config");
        fileSystem.AddFile("custom/tokazerk_config/settings.json", new MockFileData("{}"));
        var repository = new JsonSettingsRepository(fileSystem);

        var settings = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.Null(settings);
    }

    [Fact]
    public async Task LoadAsync_NormalizesUnknownVariantChoice_ToDefault()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom/tokazerk_config");
        fileSystem.AddFile(
            "custom/tokazerk_config/settings.json",
            new MockFileData(
                """
                {"fonts":{"small":10,"medium":11,"large":12,"xLarge":14,"chatSmall":10,"chatLarge":13},"variants":{"mapSize":"huge","targetWindow":"default","floatTarget":"default"}}
                """));
        var repository = new JsonSettingsRepository(fileSystem);

        var settings = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.NotNull(settings);
        Assert.Equal("default", settings!.Variants.MapSize);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFileIsUnparsable()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom/tokazerk_config");
        fileSystem.AddFile("custom/tokazerk_config/settings.json", new MockFileData("{ not json"));
        var repository = new JsonSettingsRepository(fileSystem);

        var settings = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.Null(settings);
    }

    [Fact]
    public async Task LoadAsyncIgnoresLegacyToolConfigProperties()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("custom/tokazerk_config");
        fileSystem.AddFile(
            "custom/tokazerk_config/settings.json",
            new MockFileData(
                """
                {"fonts":{"small":10,"medium":11,"large":14,"xLarge":14,"chatSmall":10,"chatLarge":13},"variants":{"mapSize":"large","targetWindow":"default","floatTarget":"default"},"checkForUpdatesOnStart":false,"updateChannel":"Beta"}
                """));
        var repository = new JsonSettingsRepository(fileSystem);

        var settings = await repository.LoadAsync("custom", CancellationToken.None);

        Assert.NotNull(settings);
        Assert.Equal(14, settings!.Fonts.Large);
        Assert.Equal("large", settings.Variants.MapSize);
    }
}
