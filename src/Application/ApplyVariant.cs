using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ApplyVariant
{
    private readonly IVariantStore _variantStore;
    private readonly ISettingsRepository _settingsRepository;

    public ApplyVariant(IVariantStore variantStore, ISettingsRepository settingsRepository)
    {
        _variantStore = variantStore;
        _settingsRepository = settingsRepository;
    }

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
            await _variantStore.ApplyAsync(customPath, variant, choice, ct).ConfigureAwait(false);

            var existing = await _settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
            var updated = existing with { Variants = existing.Variants.With(kind, choiceId) };
            await _settingsRepository.SaveAsync(customPath, updated, ct).ConfigureAwait(false);

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
