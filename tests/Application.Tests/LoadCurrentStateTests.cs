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
        var versionReader = new StubUiVersionReader();
        var useCase = new LoadCurrentState(settingsRepository, fontStore, versionReader);

        var state = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.NotNull(state.Error);
        Assert.Equal(persistedSettings, state.Settings);
        Assert.Equal("large", state.Settings.Variants.MapSize);
        Assert.Equal("purple", state.Settings.Variants.TargetWindow);
        Assert.Equal("hud", state.Settings.Variants.FloatTarget);
        Assert.Null(state.FontsInXml);
    }
}
