using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class ApplyVariantTests
{
    [Fact]
    public async Task UnknownChoiceId_ReturnsError()
    {
        var variantStore = new StubVariantStore();
        var settingsRepository = new StubSettingsRepository();
        var useCase = new ApplyVariant(variantStore, settingsRepository);

        var result = await useCase.ExecuteAsync("custom", VariantKind.MapSize, "does-not-exist", CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(variantStore.Applied);
    }

    [Fact]
    public async Task KnownChoice_AppliesAndSavesSelection()
    {
        var variantStore = new StubVariantStore();
        var settingsRepository = new StubSettingsRepository();
        var useCase = new ApplyVariant(variantStore, settingsRepository);

        var result = await useCase.ExecuteAsync("custom", VariantKind.MapSize, "large", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(variantStore.Applied);
        Assert.Equal("large", settingsRepository.Stored?.Variants.MapSize);
    }
}
