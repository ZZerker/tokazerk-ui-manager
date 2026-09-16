using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ApplyVariant(IVariantStore variantStore, ISettingsRepository settingsRepository)
{
    public async Task<ApplyResult> ExecuteAsync(string customPath, VariantKind kind, string choiceId, CancellationToken ct)
    {
        var variant = VariantTable.Get(kind);
        var choice = variant.Choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null)
        {
            return ApplyResult.Fail($"Unknown choice '{choiceId}' for {kind}.");
        }

        try
        {
            await variantStore.ApplyAsync(customPath, variant, choice, ct).ConfigureAwait(false);

            var existing = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
            var updated = existing with { Variants = existing.Variants.With(kind, choiceId) };
            await settingsRepository.SaveAsync(customPath, updated, ct).ConfigureAwait(false);

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
