using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class ApplyFontSettings
{
    private readonly IFontDefinitionStore _fontDefinitionStore;
    private readonly ISettingsRepository _settingsRepository;

    public ApplyFontSettings(IFontDefinitionStore fontDefinitionStore, ISettingsRepository settingsRepository)
    {
        _fontDefinitionStore = fontDefinitionStore;
        _settingsRepository = settingsRepository;
    }

    public async Task<ApplyResult> ExecuteAsync(string customPath, FontSettings fonts, CancellationToken ct)
    {
        var validation = fonts.Validate();
        var errors = validation.Where(w => w.IsError).ToList();
        if (errors.Count > 0)
        {
            return ApplyResult.Fail(string.Join(" ", errors.Select(e => e.Message)));
        }

        var warnings = validation.Where(w => !w.IsError).Select(w => w.Message).ToList();

        try
        {
            await _fontDefinitionStore.WriteAsync(customPath, fonts, ct).ConfigureAwait(false);

            var existing = await _settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
            var updated = existing with { Fonts = fonts };
            await _settingsRepository.SaveAsync(customPath, updated, ct).ConfigureAwait(false);

            return ApplyResult.Ok(warnings);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
