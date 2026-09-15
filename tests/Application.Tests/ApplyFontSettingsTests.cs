using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class ApplyFontSettingsTests
{
    [Fact]
    public async Task RejectsBadOrder_AndNeverThrows()
    {
        var fontStore = new StubFontDefinitionStore();
        var settingsRepository = new StubSettingsRepository();
        var useCase = new ApplyFontSettings(fontStore, settingsRepository);
        var badOrder = FontSettings.Default with { Medium = 20 };

        var result = await useCase.ExecuteAsync("custom", badOrder, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Null(fontStore.LastWritten);
    }

    [Fact]
    public async Task PropagatesClippingWarning_OnSuccess()
    {
        var fontStore = new StubFontDefinitionStore();
        var settingsRepository = new StubSettingsRepository();
        var useCase = new ApplyFontSettings(fontStore, settingsRepository);
        var warns = FontSettings.Default with { Large = 15, XLarge = 15 };

        var result = await useCase.ExecuteAsync("custom", warns, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Warnings);
        Assert.Equal(warns, fontStore.LastWritten);
    }

    [Fact]
    public async Task WritesXmlAndSavesSettings_OnSuccess()
    {
        var fontStore = new StubFontDefinitionStore();
        var settingsRepository = new StubSettingsRepository();
        var useCase = new ApplyFontSettings(fontStore, settingsRepository);
        var fonts = FontSettings.Default.With(FontTier.Small, 8);

        var result = await useCase.ExecuteAsync("custom", fonts, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(fonts, settingsRepository.Stored?.Fonts);
    }
}
