using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class LoadCurrentStateTests
{
    [Fact]
    public async Task FontStoreThrowsNotFound_YieldsError_AndDefaultSettings_NoThrow()
    {
        var settingsRepository = new StubSettingsRepository();
        var fontStore = new StubFontDefinitionStore { ThrowNotFoundOnRead = true };
        var versionReader = new StubUiVersionReader();
        var useCase = new LoadCurrentState(settingsRepository, fontStore, versionReader);

        var state = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.NotNull(state.Error);
        Assert.Equal(UiSettings.Default, state.Settings);
        Assert.Null(state.FontsInXml);
    }
}
