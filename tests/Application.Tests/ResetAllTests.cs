using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class ResetAllTests
{
    [Fact]
    public async Task WritesDefaultFonts_AndDefaultVariants_AndDefaultSettings()
    {
        var fontStore = new StubFontDefinitionStore { Stored = FontSettings.Default with { Large = 20 } };
        var variantStore = new StubVariantStore();
        var settingsRepository = new StubSettingsRepository
        {
            Stored = new UiSettings(FontSettings.Default with { Large = 20 }, new VariantSelection(MapSize: "large"), null),
        };
        var useCase = new ResetAll(fontStore, variantStore, settingsRepository);

        var result = await useCase.ExecuteAsync("custom", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(FontSettings.Default, fontStore.LastWritten);
        Assert.Equal(3, variantStore.Applied.Count);
        Assert.All(variantStore.Applied, a => Assert.Equal("default", a.Choice.Id));
        Assert.Equal(UiSettings.Default, settingsRepository.Stored);
    }
}
