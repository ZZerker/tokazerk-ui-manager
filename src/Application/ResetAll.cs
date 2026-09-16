using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ResetAll(IFontDefinitionStore fontDefinitionStore, IVariantStore variantStore, ISettingsRepository settingsRepository)
{
    public async Task<ApplyResult> ExecuteAsync(string customPath, CancellationToken ct)
    {
        try
        {
            await fontDefinitionStore.WriteAsync(customPath, FontSettings.Default, ct).ConfigureAwait(false);

            foreach (var variant in VariantTable.All)
            {
                var defaultChoice = variant.Choices.First(c => c.Id == VariantChoice.DEFAULT_ID);
                await variantStore.ApplyAsync(customPath, variant, defaultChoice, ct).ConfigureAwait(false);
            }

            await settingsRepository.SaveAsync(customPath, UiSettings.Default, ct).ConfigureAwait(false);

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
