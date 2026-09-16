using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class UpdateUiTests
{
    private static readonly ReleaseInfo Release = new(new SemVer(1, 0, 13), "v1.0.13", "TokaZerkUI-v1.0.13.zip", "https://example.test/asset.zip", "");

    private static InstallUi BuildInstallUi(StubSettingsRepository settingsRepository) =>
        new(
            new StubReleaseSource(),
            new StubUiArchiveStore(),
            new StubInstalledUiReader { InstalledUi = new InstalledUi(InstalledUiKind.Other, null) },
            settingsRepository);

    [Fact]
    public async Task ReappliesSavedFontsAndVariantsAndClearsBackupFirst()
    {
        var savedSettings = UiSettings.Default with
        {
            Fonts = FontSettings.Default.With(FontTier.Large, FontTierInfo.DefaultPx(FontTier.Large) + 4),
            Variants = new VariantSelection(MapSize: "large"),
        };
        var settingsRepository = new StubSettingsRepository { Stored = savedSettings };
        var fontDefinitionStore = new StubFontDefinitionStore();
        var variantStore = new StubVariantStore();
        var backupStore = new StubBackupStore();
        var useCase = new UpdateUi(BuildInstallUi(settingsRepository), fontDefinitionStore, variantStore, settingsRepository, backupStore);

        var result = await useCase.ExecuteAsync("custom", Release, null, CancellationToken.None);

        Assert.True(result.Install.Success);
        Assert.True(backupStore.Cleared);
        Assert.Contains("Fonts", result.Reapplied);
        Assert.Contains("MapSize", result.Reapplied);
        Assert.Equal(savedSettings.Fonts, fontDefinitionStore.LastWritten);
        Assert.Single(variantStore.Applied);
        Assert.Equal("large", variantStore.Applied[0].Choice.Id);
    }

    [Fact]
    public async Task VariantStoreFailureIsReportedWithoutStoppingFontReapply()
    {
        var savedSettings = UiSettings.Default with
        {
            Fonts = FontSettings.Default.With(FontTier.Large, FontTierInfo.DefaultPx(FontTier.Large) + 4),
            Variants = new VariantSelection(MapSize: "large"),
        };
        var settingsRepository = new StubSettingsRepository { Stored = savedSettings };
        var fontDefinitionStore = new StubFontDefinitionStore();
        var variantStore = new ThrowingVariantStore();
        var backupStore = new StubBackupStore();
        var useCase = new UpdateUi(BuildInstallUi(settingsRepository), fontDefinitionStore, variantStore, settingsRepository, backupStore);

        var result = await useCase.ExecuteAsync("custom", Release, null, CancellationToken.None);

        Assert.Contains("Fonts", result.Reapplied);
        Assert.Contains(result.Failed, f => f.StartsWith("MapSize", StringComparison.Ordinal));
    }

    private sealed class ThrowingVariantStore : IVariantStore
    {
        public Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct) =>
            throw new IOException("Could not write variant");
    }
}
