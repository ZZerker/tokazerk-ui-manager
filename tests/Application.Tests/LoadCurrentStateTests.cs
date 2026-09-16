using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class LoadCurrentStateTests
{
    [Fact]
    public async Task FontStoreThrowsNotFoundYieldsErrorAndPreservesPersistedVariantsWithoutThrow()
    {
        var persistedSettings = UiSettings.Default with
        {
            Variants = new VariantSelection(MapSize: "large", TargetWindow: "purple", FloatTarget: "hud")
        };
        var settingsRepository = new StubSettingsRepository { Stored = persistedSettings };
        var fontStore = new StubFontDefinitionStore { ThrowNotFoundOnRead = true };
        var installedUiReader = new StubInstalledUiReader();
        var useCase = new LoadCurrentState(settingsRepository, fontStore, installedUiReader);

        var state = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.NotNull(state.FontError);
        Assert.Null(state.SettingsError);
        Assert.Null(state.IdentityError);
        Assert.Equal(persistedSettings, state.Settings);
        Assert.Equal("large", state.Settings.Variants.MapSize);
        Assert.Equal("purple", state.Settings.Variants.TargetWindow);
        Assert.Equal("hud", state.Settings.Variants.FloatTarget);
        Assert.Null(state.FontsInXml);
    }

    [Fact]
    public async Task SettingsReadFailureIsCategorizedAndFontStateStillLoads()
    {
        var settingsRepository = new StubSettingsRepository { LoadException = new IOException("Settings unavailable") };
        var fontStore = new StubFontDefinitionStore();
        var installedUiReader = new StubInstalledUiReader();
        var useCase = new LoadCurrentState(settingsRepository, fontStore, installedUiReader);

        var state = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.Equal("Settings unavailable", state.SettingsError);
        Assert.Null(state.FontError);
        Assert.Null(state.IdentityError);
        Assert.Equal(UiSettings.Default, state.Settings);
        Assert.Equal(FontSettings.Default, state.FontsInXml);
    }

    [Fact]
    public async Task OtherUiDoesNotLoadConfiguration()
    {
        var settingsRepository = new StubSettingsRepository { LoadException = new IOException("Must not be read") };
        var fontStore = new StubFontDefinitionStore { ThrowNotFoundOnRead = true };
        var installedUiReader = new StubInstalledUiReader
        {
            InstalledUi = new InstalledUi(InstalledUiKind.Other, null)
        };
        var useCase = new LoadCurrentState(settingsRepository, fontStore, installedUiReader);

        var state = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.Equal(InstalledUiKind.Other, state.InstalledUi.Kind);
        Assert.Equal(UiSettings.Default, state.Settings);
        Assert.Null(state.FontsInXml);
        Assert.Null(state.SettingsError);
        Assert.Null(state.FontError);
    }
}
