using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class LoadCurrentState
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFontDefinitionStore _fontDefinitionStore;
    private readonly IUiVersionReader _uiVersionReader;

    public LoadCurrentState(ISettingsRepository settingsRepository, IFontDefinitionStore fontDefinitionStore, IUiVersionReader uiVersionReader)
    {
        _settingsRepository = settingsRepository;
        _fontDefinitionStore = fontDefinitionStore;
        _uiVersionReader = uiVersionReader;
    }

    public async Task<CurrentState> ExecuteAsync(string customPath, CancellationToken ct)
    {
        try
        {
            var settings = await _settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;
            var fontsInXml = await _fontDefinitionStore.ReadAsync(customPath, ct).ConfigureAwait(false);
            var version = settings.InstalledUiVersion ?? await _uiVersionReader.ReadAsync(customPath, ct).ConfigureAwait(false);

            return new CurrentState(settings, fontsInXml, version, null);
        }
        catch (FontDefinitionsNotFoundException ex)
        {
            return new CurrentState(UiSettings.Default, null, null, ex.Message);
        }
        catch (IOException ex)
        {
            return new CurrentState(UiSettings.Default, null, null, ex.Message);
        }
    }
}
