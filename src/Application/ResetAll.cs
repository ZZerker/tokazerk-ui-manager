using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ResetAll
{
    private readonly IFontDefinitionStore _fontDefinitionStore;
    private readonly IVariantStore _variantStore;
    private readonly ISettingsRepository _settingsRepository;

    public ResetAll(IFontDefinitionStore fontDefinitionStore, IVariantStore variantStore, ISettingsRepository settingsRepository)
    {
        _fontDefinitionStore = fontDefinitionStore;
        _variantStore = variantStore;
        _settingsRepository = settingsRepository;
    }

    public async Task<ApplyResult> ExecuteAsync(string customPath, CancellationToken ct)
    {
        try
        {
            await _fontDefinitionStore.WriteAsync(customPath, FontSettings.Default, ct).ConfigureAwait(false);

            foreach (var variant in VariantTable.All)
            {
                var defaultChoice = variant.Choices.First(c => c.Id == VariantChoice.DefaultId);
                await _variantStore.ApplyAsync(customPath, variant, defaultChoice, ct).ConfigureAwait(false);
            }

            await _settingsRepository.SaveAsync(customPath, UiSettings.Default, ct).ConfigureAwait(false);

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
